using Habbo.Domain.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Silo.Rooms;

/// <summary>Owns a room's avatars: registration, pathing, per-tick stepping, and what they are
/// doing — sitting, lying, dancing, holding up a sign, asleep. Produces the avatar updates the
/// grain broadcasts, and notes every change of posture or state for the grain to pass on (to the
/// room's other clients and its wired); it never sends anything itself.</summary>
public sealed class RoomEntityStore(RoomModel model, RoomItemStore items)
{
    /// <summary>Whether walks may cut across tiles diagonally (the room's <c>AllowDiagonal</c>).</summary>
    public bool AllowDiagonal { get; set; } = true;

    /// <summary>How many ticks a sign is in the status: the one that tells it, and the one that takes it down
    /// (Habbo: "sign 7" in one update, gone in the next, 500 ms later).</summary>
    public const int SignTicks = 2;

    /// <summary>The room index of someone without an avatar here: a spectator.</summary>
    public const int NoRoomIndex = -1;

    /// <summary>The status the client shows a trading avatar with.</summary>
    private const string TradingStatus = "trd";

    /// <summary>What ends each status entry, and opens and closes the line.</summary>
    private const string StatusSeparator = "/";

    /// <summary>How many ticks after a roller's slide the rider's status is told.</summary>
    private const int RolledUpdates = 2;

    /// <summary>An idle time that never comes.</summary>
    public const int Never = 0;

    /// <summary>How many ticks an avatar stays idle before it falls asleep (the room's setting); Never for not at all.</summary>
    public int SleepAfterTicks { get; set; } = Never;

    /// <summary>How many ticks an avatar stays idle before it is sent out of the room; Never for not at all.</summary>
    public int SendOutAfterTicks { get; set; } = Never;

    private readonly Dictionary<Guid, RoomEntity> _entities = [];
    private readonly Dictionary<int, RoomEntity> _bots = [];
    private readonly Dictionary<int, RoomEntity> _pets = [];
    private readonly List<(RoomEntity Entity, EntityChange Change)> _changes = [];
    private int _nextRoomIndex = 1;

    /// <summary>The room's users.</summary>
    public IReadOnlyCollection<RoomEntity> Entities => _entities.Values;

    /// <summary>How many users are in the room; bots do not count.</summary>
    public int Count => _entities.Count;

    public IReadOnlyCollection<RoomEntity> Bots => _bots.Values;

    public IReadOnlyCollection<RoomEntity> Pets => _pets.Values;

    /// <summary>Every avatar in the room: its users, then its bots and pets.</summary>
    public IEnumerable<RoomEntity> Avatars => _entities.Values.Concat(_bots.Values).Concat(_pets.Values);

    /// <summary>Ticks since the room started: the clock <see cref="RoomEntity.LastActionTick"/> reads.</summary>
    public int CurrentTick { get; private set; }

    public RoomEntity? Find(Guid playerId) => _entities.GetValueOrDefault(playerId);

    /// <summary>The user with the room index.</summary>
    public RoomEntity? FindByIndex(int roomIndex) => _entities.Values.FirstOrDefault(entity => entity.RoomIndex == roomIndex);

    public RoomEntity? FindBot(int botId) => _bots.GetValueOrDefault(botId);

    public RoomEntity? FindPet(int petId) => _pets.GetValueOrDefault(petId);

    public bool HasAvatarAt(int x, int y) => Avatars.Any(e => e.X == x && e.Y == y);

    /// <summary>What happened to avatars since the grain last asked, in order.</summary>
    public List<(RoomEntity Entity, EntityChange Change)> TakeChanges()
    {
        var changes = _changes.ToList();
        _changes.Clear();
        return changes;
    }

    /// <summary>Registers the player at the door, or re-binds an existing entity when a
    /// previous session lingers (a crashed client re-entering).</summary>
    public RoomEntity Enter(Guid playerId, Guid sessionGuid, PlayerData player, int doorDirection)
    {
        if (_entities.TryGetValue(playerId, out var existing))
        {
            existing.SessionGuid = sessionGuid;
            existing.Player = player;
            PlaceAtDoor(existing, doorDirection);
            return existing;
        }

        var entity = new RoomEntity
        {
            Player = player,
            SessionGuid = sessionGuid,
            RoomIndex = _nextRoomIndex++,
            X = model.DoorX,
            Y = model.DoorY,
            Z = model.DoorHeight,
            HeadDirection = doorDirection,
            BodyDirection = doorDirection,
        };
        _entities[playerId] = entity;
        return entity;
    }

    public RoomEntity? Leave(Guid playerId) =>
        _entities.Remove(playerId, out var entity) ? entity : null;

    /// <summary>Stands a bot on a tile.</summary>
    public RoomEntity AddBot(RoomBot bot, int x, int y, double z, int direction)
    {
        var entity = new RoomEntity
        {
            Player = bot.Avatar(),
            Bot = bot,
            SessionGuid = Guid.Empty,
            RoomIndex = _nextRoomIndex++,
            X = x,
            Y = y,
            Z = z,
            HeadDirection = direction,
            BodyDirection = direction,
            DanceStyle = bot.Data.DanceStyle,
        };
        _bots[bot.Data.Id] = entity;
        return entity;
    }

    public RoomEntity? RemoveBot(int botId) =>
        _bots.Remove(botId, out var entity) ? entity : null;

    /// <summary>Stands a pet on a tile.</summary>
    public RoomEntity AddPet(RoomPet pet, string figure, int x, int y, double z, int direction)
    {
        var entity = new RoomEntity
        {
            Player = pet.Avatar(figure),
            Pet = pet,
            SessionGuid = Guid.Empty,
            RoomIndex = _nextRoomIndex++,
            X = x,
            Y = y,
            Z = z,
            HeadDirection = direction,
            BodyDirection = direction,
        };
        _pets[pet.Data.Id] = entity;
        return entity;
    }

    public RoomEntity? RemovePet(int petId) =>
        _pets.Remove(petId, out var entity) ? entity : null;

    /// <summary>Takes the pet out of any posture it holds; true when it held one.</summary>
    public bool ClearPosture(RoomEntity entity)
    {
        if (entity.Pet is not { Posture: not null } pet)
            return false;

        pet.Posture = null;
        pet.PostureUntilTick = 0;
        return true;
    }

    /// <summary>A free tile to stand on, anywhere the avatar can walk to; null when there is none.</summary>
    public (int X, int Y)? RandomReachableTile(RoomEntity entity, Random random)
    {
        var tiles = new List<(int X, int Y)>();
        var occupied = OccupiedTiles(entity);
        for (var x = 0; x < model.SizeX; x++)
        for (var y = 0; y < model.SizeY; y++)
        {
            if (model.IsTile(x, y) && (x != entity.X || y != entity.Y) && !items.BlocksWalking(x, y, IsClubMember(entity), entity.GroupIds) && !occupied.Contains((x, y))
                && !(x == model.DoorX && y == model.DoorY))
                tiles.Add((x, y));
        }

        return tiles.Count == 0 ? null : tiles[random.Next(tiles.Count)];
    }

    /// <summary>Sends the entity walking to the tile, or as close to it as it can get.
    /// Returns false when the click is a no-op (its own tile, or nowhere it can move to) so
    /// the grain can stay silent.</summary>
    public bool Walk(RoomEntity entity, int x, int y)
    {
        if (entity.Frozen || !model.IsTile(x, y) || (entity.X == x && entity.Y == y))
            return false;

        // one sent to a bed lies down at its head, in the lane clicked (Habbo: a click on a bed's foot walked to the
        // pillow end of that lane)
        if (items.BedHeadFor(x, y) is { } head)
            (x, y) = head;
        if (entity.X == x && entity.Y == y)
            return false;

        // A new click re-paths from the current tile; steps are never interrupted mid-slide.
        entity.Goal = (x, y);
        if (!Repath(entity))
        {
            entity.Goal = null;
            return false;
        }

        GetUp(entity);
        ClearPosture(entity);
        return true;
    }

    /// <summary>Routes the entity to its goal from where it stands, settling for the closest tile
    /// it can still reach when somebody is on the goal or in the way. False when it cannot move
    /// at all, which leaves the path empty so the next step finishes the walk where it stands.</summary>
    private bool Repath(RoomEntity entity)
    {
        entity.Path.Clear();
        if (entity.Goal is not { } goal)
            return false;

        var path = Pathfinder.FindPathToNearest(model, (entity.X, entity.Y), goal, Blocks(entity, goal), AllowDiagonal);

        // A path of one tile is the tile it is already on: it is as close as it can get.
        if (path is null || path.Count <= 1)
            return false;

        entity.Path = new Queue<(int X, int Y)>(path.Skip(1));
        entity.Walking = true;
        return true;
    }

    /// <summary>What the entity cannot walk onto. Only where avatars actually stand counts —
    /// nothing is reserved along a path, so two of them may head for the same tile and whoever
    /// arrives first keeps it. A seat or a bed is somewhere to end a walk, not to pass over: Habbo walks round a bed
    /// to the tile it was sent to (measured), so only the goal may be one.</summary>
    private Func<int, int, bool> Blocks(RoomEntity entity, (int X, int Y) goal)
    {
        // the pathfinder asks about every tile it tries: where the others stand is gathered once, not per tile
        var occupied = OccupiedTiles(entity);
        var clubMember = IsClubMember(entity);
        return (x, y) => (x != entity.X || y != entity.Y)
                         && (occupied.Contains((x, y)) || items.BlocksWalking(x, y, clubMember, entity.GroupIds)
                             || ((x, y) != goal && items.SeatOrBedAt(x, y)));
    }

    /// <summary>Whether the entity is a club member's avatar, whom club gates let through.</summary>
    private static bool IsClubMember(RoomEntity entity) =>
        entity.Bot is null && entity.Pet is null && entity.Player.ClubLevel != Habbo.Domain.Users.ClubLevel.None;

    /// <summary>Whether somebody else is standing on the tile.</summary>
    private bool Occupied(RoomEntity entity, int x, int y) =>
        Avatars.Any(other => IsOther(entity, other) && other.X == x && other.Y == y);

    /// <summary>The tiles somebody else is standing on, for asking about many tiles at once.</summary>
    private HashSet<(int X, int Y)> OccupiedTiles(RoomEntity entity)
    {
        var tiles = new HashSet<(int X, int Y)>();
        foreach (var other in Avatars)
        {
            if (IsOther(entity, other))
                tiles.Add((other.X, other.Y));
        }

        return tiles;
    }

    /// <summary>Somebody who can stand in the entity's way: not itself, nor what it rides or who rides it.</summary>
    private static bool IsOther(RoomEntity entity, RoomEntity other) =>
        other != entity && other != entity.Mount && other != entity.Rider;

    /// <summary>Advances every walking entity one tile, takes down signs that have been up long
    /// enough, and puts to sleep whoever has been idle too long. The update's position is the
    /// tile being LEFT and "mv" the tile being entered — the client slides 500ms per step.</summary>
    public List<AvatarUpdateData> Tick()
    {
        CurrentTick++;
        var updates = new List<AvatarUpdateData>();
        foreach (var entity in Avatars.ToList())
        {
            var (fromX, fromY, fromZ) = (entity.X, entity.Y, entity.Z);
            // the tick after a roller's slide finishes it: a walk asked for since the slide is dropped, and the
            // rider stands where the roller put them (Habbo's own, measured: such clicks never walk; one sent after
            // this tick walks on the next, as any does)
            var heldBySlide = entity.RolledUpdatesLeft == RolledUpdates;
            if (heldBySlide)
                StopWalking(entity);
            var update = heldBySlide ? null : Step(entity);

            // whoever rides a pet goes where it goes
            if (entity.Rider is { } rider && update is not null)
            {
                rider.X = entity.X;
                rider.Y = entity.Y;
                rider.Z = entity.Z;
                rider.HeadDirection = entity.HeadDirection;
                rider.BodyDirection = entity.BodyDirection;
                var moved = entity.X != fromX || entity.Y != fromY;
                updates.Add(ToUpdate(rider, fromX, fromY, fromZ,
                    StatusOf(rider, moved ? $"mv {entity.X},{entity.Y},{RoomItemStore.FormatHeight(entity.Z)}" : null)));
            }

            // a sign held up is told on the tick after, and taken down on the next
            if (entity.Sign is not null)
            {
                if (--entity.SignTicksLeft <= 0)
                    entity.Sign = null;
                update ??= CurrentStatus(entity);
            }

            if (entity.Pet is { Posture: not null, PostureUntilTick: > 0 } pet && CurrentTick >= pet.PostureUntilTick)
            {
                ClearPosture(entity);
                update ??= CurrentStatus(entity);
            }

            // after a roller's slide: where the rider now stands, on this tick and the next. The first carries an
            // empty status besides ("/flatctrl 4///" on Habbo); on the second a walk begun tells it instead.
            if (entity.RolledUpdatesLeft > 0)
            {
                update ??= ToUpdate(entity, entity.X, entity.Y, entity.Z,
                    StatusOf(entity, afterSlide: heldBySlide));
                entity.RolledUpdatesLeft = entity.Walking && !heldBySlide ? 0 : entity.RolledUpdatesLeft - 1;
            }

            // a bot or a pet never nods off, or overstays, like a user does
            if (entity.Bot is null && entity.Pet is null)
            {
                entity.IdleTicks++;
                if (entity.IdleTicks == SleepAfterTicks && !entity.Asleep)
                {
                    entity.Asleep = true;
                    _changes.Add((entity, EntityChange.FellAsleep));
                }

                if (entity.IdleTicks == SendOutAfterTicks)
                    _changes.Add((entity, EntityChange.IdledOut));
            }

            if (update is not null)
                updates.Add(update);
        }

        return updates;
    }

    private AvatarUpdateData? Step(RoomEntity entity)
    {
        if (!entity.Walking)
            return null;

        // Somebody may have taken the next tile since the path was made. Go round them, or stop
        // here if there is no way round: the tile belongs to whoever reached it first. Avatars
        // interrupt a walk, and so does a gate shut since — it guards what is behind it — but other
        // furniture put down in front of someone is walked through, because the path was already
        // theirs when it was made.
        if (entity.Path.Count > 0 && entity.Path.Peek() is var ahead
            && (Occupied(entity, ahead.X, ahead.Y) || items.ClosedGateAt(ahead.X, ahead.Y)))
            Repath(entity);

        if (entity.Path.Count == 0)
        {
            entity.Walking = false;
            entity.Goal = null;
            _changes.Add((entity, EntityChange.Arrived));

            // Arriving on a seat or a bed applies it — the client never asks for either. A pet
            // stands on furniture the way it stands anywhere.
            if (entity.Pet is not null)
                return CurrentStatus(entity);
            if (items.SeatHeightAt(entity.X, entity.Y) is not null)
                SitDown(entity);
            else if (items.BedHeightAt(entity.X, entity.Y) is not null)
                LieDown(entity);

            return CurrentStatus(entity);
        }

        var next = entity.Path.Dequeue();
        var (fromX, fromY, fromZ) = (entity.X, entity.Y, entity.Z);

        entity.BodyDirection = DirectionOf(fromX, fromY, next.X, next.Y);
        entity.HeadDirection = entity.BodyDirection;
        entity.X = next.X;
        entity.Y = next.Y;
        entity.Z = items.AvatarHeightAt(next.X, next.Y);

        var target = $"{next.X},{next.Y},{RoomItemStore.FormatHeight(entity.Z)}";
        return ToUpdate(entity, fromX, fromY, fromZ, StatusOf(entity, $"mv {target}"));
    }

    /// <summary>Sits or stands the entity if the tile allows it; null when nothing changes.</summary>
    public AvatarUpdateData? ChangePosture(RoomEntity entity, int posture)
    {
        if (posture == 1)
        {
            // one crossing a seat does not sit on it on the way: the seat would stay theirs where the walk ends
            if (entity.Sitting || entity.Walking || items.SeatHeightAt(entity.X, entity.Y) is null)
                return null;

            SitDown(entity);
            return CurrentStatus(entity);
        }

        if (posture != 0 || !entity.Sitting)
            return null;

        GetUp(entity);
        return CurrentStatus(entity);
    }

    private void SitDown(RoomEntity entity)
    {
        FaceForPosture(entity, items.SeatAt(entity.X, entity.Y));
        entity.Sitting = true;
        entity.Laying = false;
        StopDancing(entity);
        _changes.Add((entity, EntityChange.SatDown));
    }

    private void LieDown(RoomEntity entity)
    {
        FaceForPosture(entity, items.BedAt(entity.X, entity.Y));
        entity.Laying = true;
        entity.Sitting = false;
        StopDancing(entity);
        _changes.Add((entity, EntityChange.LayDown));
    }

    /// <summary>Turns an avatar sitting or lying down the way the seat or bed under it faces; on the floor, the way it
    /// already faced, but straight: the client has no diagonal sitting or lying pose, so a diagonal turns back to the
    /// straight direction before it, as the hotel does.</summary>
    private static void FaceForPosture(RoomEntity entity, RoomItemStore.PlacedItem? furniture)
    {
        var direction = furniture?.Rotation ?? entity.BodyDirection;
        direction -= direction % 2;
        entity.BodyDirection = direction;
        entity.HeadDirection = direction;
    }

    /// <summary>How far above the floor an avatar lying on it is drawn, as the client places it.</summary>
    private const double FloorPostureHeight = 0.5;

    /// <summary>How far above the floor an avatar sitting on it is drawn: Habbo sends "sit 0.55 1".</summary>
    private const double FloorSitHeight = 0.55;

    /// <summary>Sits the avatar down where it stands, on a seat or on the floor (:sit); null when it cannot.</summary>
    public AvatarUpdateData? SitHere(RoomEntity entity)
    {
        if (entity.Sitting || entity.Walking || entity.Frozen)
            return null;

        SitDown(entity);
        return CurrentStatus(entity);
    }

    /// <summary>Lays the avatar down where it stands, on a bed or on the floor (:lay); null when it cannot.</summary>
    public AvatarUpdateData? LayHere(RoomEntity entity)
    {
        if (entity.Laying || entity.Walking || entity.Frozen)
            return null;

        LieDown(entity);
        return CurrentStatus(entity);
    }

    /// <summary>Stands the avatar up from sitting or lying (:stand); null when it was standing.</summary>
    public AvatarUpdateData? StandHere(RoomEntity entity)
    {
        if (!entity.Sitting && !entity.Laying)
            return null;

        GetUp(entity);
        return CurrentStatus(entity);
    }

    /// <summary>Stands the entity up from a seat or a bed, if it was on one.</summary>
    private void GetUp(RoomEntity entity)
    {
        if (!entity.Sitting && !entity.Laying)
            return;

        entity.Sitting = false;
        entity.Laying = false;
        _changes.Add((entity, EntityChange.StoodUp));
    }

    /// <summary>Starts, changes or (style 0) stops the entity dancing. Nobody dances sitting or
    /// lying down; false when nothing changes.</summary>
    public bool Dance(RoomEntity entity, int style)
    {
        if (style == entity.DanceStyle || (style != 0 && (entity.Sitting || entity.Laying)))
            return false;

        entity.DanceStyle = style;
        return true;
    }

    private void StopDancing(RoomEntity entity)
    {
        if (entity.DanceStyle == 0)
            return;

        entity.DanceStyle = 0;
        _changes.Add((entity, EntityChange.DanceStopped));
    }

    /// <summary>Holds up a sign: the next tick tells it, the one after takes it down again, as Habbo does (the client
    /// keeps showing it for its own while).</summary>
    public void ShowSign(RoomEntity entity, int sign)
    {
        entity.Sign = sign;
        entity.SignTicksLeft = SignTicks;
    }

    /// <summary>The entity did something: it is not idle. True when that woke it up.</summary>
    public bool Wake(RoomEntity entity)
    {
        entity.IdleTicks = 0;
        if (!entity.Asleep)
            return false;

        entity.Asleep = false;
        return true;
    }

    /// <summary>Puts the entity to sleep at once, as ":idle" does; false when it already was.</summary>
    public bool Sleep(RoomEntity entity)
    {
        if (entity.Asleep)
            return false;

        entity.Asleep = true;
        return true;
    }

    /// <summary>Notes an action the entity just performed, for the wired that asks what it is doing.</summary>
    public void RecordAction(RoomEntity entity, int action, string extra)
    {
        entity.LastAction = action;
        entity.LastActionExtra = extra;
        entity.LastActionTick = CurrentTick;
    }

    /// <summary>The entity's standing state as an update — what a newcomer needs to see
    /// (the Users packet carries positions, not postures).</summary>
    public AvatarUpdateData CurrentStatus(RoomEntity entity) =>
        ToUpdate(entity, entity.X, entity.Y, entity.Z, StatusOf(entity));

    /// <summary>The status line as the client reads it and Habbo writes it: "key value" entries, each followed by a
    /// slash, after a leading one. The rights and a move come first, then the posture, which is always there, empty
    /// for one standing or walking, then a sign and the like: "//", "/flatctrl 4/mv 7,15,0.0//",
    /// "/flatctrl 4/sit 0.55 1/sign 7/". Just after a roller's slide Habbo adds an empty entry before the posture.</summary>
    private string StatusOf(RoomEntity entity, string? move = null, bool afterSlide = false)
    {
        var parts = new List<string>();
        if (entity.ControllerLevel > 0)
            parts.Add($"flatctrl {entity.ControllerLevel}");
        if (move is not null)
            parts.Add(move);
        if (afterSlide)
            parts.Add(string.Empty);

        parts.Add(move is not null ? string.Empty
            : entity.Sitting ? SitStatus(entity)
            : entity.Laying ? LayStatus(items.BedHeightAt(entity.X, entity.Y) ?? FloorPostureHeight)
            : string.Empty);

        if (move is null && entity.Pet?.Posture is { } posture)
            parts.Add(posture);
        if (entity.Sign is { } sign)
            parts.Add($"sign {sign}");
        if (entity.Trading)
            parts.Add(TradingStatus);

        return StatusSeparator + string.Concat(parts.Select(part => part + StatusSeparator));
    }

    public RoomEntityData ToWireData(RoomEntity entity) => entity.Pet is { } pet
        ? new(
            entity.RoomIndex,
            pet.Data.Id,
            pet.Data.Name,
            Motto: string.Empty,
            entity.Figure,
            Sex: string.Empty,
            entity.X,
            entity.Y,
            RoomItemStore.FormatHeight(entity.Z),
            entity.HeadDirection,
            RoomEntityType.Pet,
            GroupId: FavouriteGroup.None,
            GroupStatus: FavouriteGroup.None,
            GroupName: string.Empty,
            SwimFigure: string.Empty,
            AchievementScore: 0,
            IsModerator: false,
            pet.OwnerAccountId,
            pet.OwnerName,
            BotSkills: null,
            pet.Type.Id,
            pet.Data.Level,
            pet.Posture ?? string.Empty,
            pet.Data.SaddleDefinitionId is not null,
            entity.Rider is not null,
            pet.Data.BreedingAllowed,
            pet.Data.Rarity,
            pet.CanBreed,
            pet.CanHarvest,
            pet.CanRevive)
        : entity.Bot is { } bot
        ? new(
            entity.RoomIndex,
            bot.Data.Id,
            bot.Data.Name,
            bot.Data.Motto,
            entity.Figure,
            bot.Data.Gender.ToLetter(),
            entity.X,
            entity.Y,
            RoomItemStore.FormatHeight(entity.Z),
            entity.HeadDirection,
            RoomEntityType.BotWithSkills,
            GroupId: FavouriteGroup.None,
            GroupStatus: FavouriteGroup.None,
            GroupName: string.Empty,
            SwimFigure: string.Empty,
            AchievementScore: 0,
            IsModerator: false,
            bot.OwnerAccountId,
            bot.OwnerName,
            bot.Skills)
        : new(
        entity.RoomIndex,
        entity.Player.AccountId,
        entity.Player.Username,
        entity.Player.Motto,
        entity.Figure,
        entity.Player.Gender.ToLetter(),
        entity.X,
        entity.Y,
        RoomItemStore.FormatHeight(entity.Z),
        entity.HeadDirection,
        RoomEntityType.User,
        GroupId: entity.FavouriteGroup?.Id ?? FavouriteGroup.None,
        GroupStatus: entity.FavouriteGroup?.Status ?? FavouriteGroup.None,
        GroupName: entity.FavouriteGroup?.Name ?? string.Empty,
        SwimFigure: string.Empty,
        entity.Player.AchievementScore,
        entity.Player.IsModerator,
        BadgesRank: entity.BadgesRank);

    /// <summary>Moves the entity one step onto a tile that is free to stand on, sliding there as a
    /// walking step does; null when it cannot go there.</summary>
    public AvatarUpdateData? Slide(RoomEntity entity, int x, int y)
    {
        if (!model.IsTile(x, y) || Occupied(entity, x, y) || items.BlocksWalking(x, y, IsClubMember(entity), entity.GroupIds))
            return null;

        var (fromX, fromY, fromZ) = (entity.X, entity.Y, entity.Z);
        entity.Path.Clear();
        entity.Goal = null;
        entity.Walking = false;
        GetUp(entity);
        entity.X = x;
        entity.Y = y;
        entity.Z = items.AvatarHeightAt(x, y);
        return ToUpdate(entity, fromX, fromY, fromZ, StatusOf(entity, $"mv {x},{y},{RoomItemStore.FormatHeight(entity.Z)}"));
    }

    /// <summary>Moves the entity one step into a tile nobody may walk onto, such as a teleport it goes into,
    /// sliding there as a walking step does.</summary>
    public AvatarUpdateData StepInto(RoomEntity entity, int x, int y)
    {
        var (fromX, fromY, fromZ) = (entity.X, entity.Y, entity.Z);
        entity.Path.Clear();
        entity.Goal = null;
        entity.Walking = false;
        GetUp(entity);
        entity.X = x;
        entity.Y = y;
        entity.Z = items.AvatarHeightAt(x, y);
        return ToUpdate(entity, fromX, fromY, fromZ, StatusOf(entity, $"mv {x},{y},{RoomItemStore.FormatHeight(entity.Z)}"));
    }

    /// <summary>Turns the entity to face a tile, as a click on someone does: a standing entity turns
    /// all of itself, a sitting one only its head, a lying or walking one not at all. Null when
    /// nothing turned.</summary>
    public AvatarUpdateData? LookTo(RoomEntity entity, int x, int y)
    {
        if ((entity.X == x && entity.Y == y) || entity.Laying || entity.Path.Count > 0)
            return null;

        var direction = DirectionOf(entity.X, entity.Y, x, y);
        if (entity.HeadDirection == direction && (entity.Sitting || entity.BodyDirection == direction))
            return null;

        entity.HeadDirection = direction;
        if (!entity.Sitting)
            entity.BodyDirection = direction;
        return CurrentStatus(entity);
    }

    /// <summary>Settles an avatar sitting or lying again after the furniture under it changed: turned the way a seat or
    /// bed rotated to, at the height it now offers, or standing up when it went. Null when it neither sits nor lies.</summary>
    public AvatarUpdateData? Resettle(RoomEntity entity)
    {
        if (!entity.Sitting && !entity.Laying)
            return null;

        var furniture = entity.Sitting ? items.SeatAt(entity.X, entity.Y) : items.BedAt(entity.X, entity.Y);
        if (furniture is not null)
            FaceForPosture(entity, furniture);
        else
            GetUp(entity);
        entity.Z = items.AvatarHeightAt(entity.X, entity.Y);
        return CurrentStatus(entity);
    }

    /// <summary>Carries the entity along with furni that moved under it: onto the tile, at the
    /// height there, in whatever posture it had.</summary>
    public void Carry(RoomEntity entity, int x, int y)
    {
        // a tile the room does not have (furni turned so its far end hangs over the void) keeps them where they were
        if (!model.IsTile(x, y))
            return;

        // a walk planned from where they stood would go on from a tile no longer next to them
        entity.Path.Clear();
        entity.Goal = null;
        entity.Walking = false;
        entity.X = x;
        entity.Y = y;
        entity.Z = items.AvatarHeightAt(x, y);
    }

    /// <summary>Drops whatever walk the entity was on, where it stands.</summary>
    private static void StopWalking(RoomEntity entity)
    {
        entity.Path.Clear();
        entity.Goal = null;
        entity.Walking = false;
    }

    /// <summary>A roller slides the entity one tile on: carried there, and its status told on each of the next two
    /// ticks, as Habbo does.</summary>
    public void Roll(RoomEntity entity, int x, int y)
    {
        Carry(entity, x, y);
        entity.RolledUpdatesLeft = RolledUpdates;
    }

    /// <summary>Turns the entity to face a direction.</summary>
    public AvatarUpdateData Turn(RoomEntity entity, int direction)
    {
        entity.BodyDirection = direction;
        entity.HeadDirection = direction;
        return CurrentStatus(entity);
    }

    /// <summary>Puts the entity straight onto a tile, as a wired teleport does: any walk is
    /// dropped, and it sits or lies down if the tile is a seat or a bed. Returns the update that
    /// shows it there.</summary>
    public AvatarUpdateData Warp(RoomEntity entity, int x, int y, (int X, int Y)? keepWalkingTo = null)
    {
        // only onto the room's floor: wired can name any tile (a position variable, a wall item's 0,0)
        if (!model.IsTile(x, y))
            return CurrentStatus(entity);

        entity.X = x;
        entity.Y = y;
        entity.Z = items.AvatarHeightAt(x, y);
        entity.Path.Clear();
        entity.Goal = null;
        entity.Walking = false;
        if (keepWalkingTo is { } goal)
        {
            entity.Goal = goal;
            Repath(entity);
        }

        GetUp(entity);
        if (entity.Pet is not null)
            return CurrentStatus(entity);
        if (items.SeatHeightAt(x, y) is not null)
            SitDown(entity);
        else if (items.BedHeightAt(x, y) is not null)
            LieDown(entity);
        return CurrentStatus(entity);
    }

    private void PlaceAtDoor(RoomEntity entity, int doorDirection)
    {
        entity.X = model.DoorX;
        entity.Y = model.DoorY;
        entity.Z = model.DoorHeight;
        entity.HeadDirection = doorDirection;
        entity.BodyDirection = doorDirection;
        entity.Path.Clear();
        entity.Walking = false;
        entity.Sitting = false;
        entity.Laying = false;
    }

    /// <summary>"sit &lt;height&gt; &lt;canStandUp&gt;": the third token enables the stand button, for one sitting on
    /// the floor; one on a seat gets up by walking off it (Habbo: "sit 1.0 0" on a chair).</summary>
    private string SitStatus(RoomEntity entity)
    {
        var (height, seated) = SitHeight(entity);
        return $"sit {RoomItemStore.FormatHeight(height)} {(seated ? 0 : 1)}";
    }

    /// <summary>How far above its height a sitting avatar is drawn: the seat's sit height, or the floor's.</summary>
    private (double Height, bool Seated) SitHeight(RoomEntity entity) => items.SeatHeightAt(entity.X, entity.Y) is { } seat
        ? (seat, true)
        : (FloorSitHeight, false);

    /// <summary>The height the avatar is drawn at: its own, and a sitting one's sit height above it. A roller slides an
    /// avatar at this height; Habbo slides one sitting on a roller (0.45 high, sitting 0.55 above it) from 1.0 to 1.0,
    /// so it does not sink for the slide.</summary>
    public double DrawnHeight(RoomEntity entity) => entity.Sitting ? entity.Z + SitHeight(entity).Height : entity.Z;

    /// <summary>"lay &lt;height&gt; null", as Habbo writes it (the client reads only the height).</summary>
    private static string LayStatus(double height) => $"lay {RoomItemStore.FormatHeight(height)} null";

    private static AvatarUpdateData ToUpdate(RoomEntity entity, int x, int y, double z, string status) =>
        new(entity.RoomIndex, x, y, RoomItemStore.FormatHeight(z), entity.HeadDirection, entity.BodyDirection, status);

    /// <summary>Habbo direction wheel: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW.</summary>
    /// <summary>The step one tile in a direction takes (0 is north, clockwise in eighths).</summary>
    public static (int X, int Y) Offset(int direction) => (direction % 8) switch
    {
        0 => (0, -1),
        1 => (1, -1),
        2 => (1, 0),
        3 => (1, 1),
        4 => (0, 1),
        5 => (-1, 1),
        6 => (-1, 0),
        _ => (-1, -1),
    };

    private static int DirectionOf(int fromX, int fromY, int toX, int toY) =>
        (Math.Sign(toX - fromX), Math.Sign(toY - fromY)) switch
        {
            (0, -1) => 0,
            (1, -1) => 1,
            (1, 0) => 2,
            (1, 1) => 3,
            (0, 1) => 4,
            (-1, 1) => 5,
            (-1, 0) => 6,
            (-1, -1) => 7,
            _ => 2,
        };

    /// <summary>What happened to an avatar that the rest of the room should hear about.</summary>
    public enum EntityChange
    {
        SatDown,
        LayDown,
        StoodUp,
        DanceStopped,
        FellAsleep,

        /// <summary>A walk ended, where it was going or as close as it got.</summary>
        Arrived,

        /// <summary>A user stood idle for as long as the room lets them.</summary>
        IdledOut,
    }

    /// <summary>An avatar in the room, with its live position, walk state and what it is doing.</summary>
    public sealed class RoomEntity
    {
        /// <summary>The user's account; for a bot, its name, look and motto (<see cref="RoomBot.Avatar"/>).</summary>
        public required PlayerData Player { get; set; }

        /// <summary>The bot this avatar is; null for a user.</summary>
        public RoomBot? Bot { get; init; }

        /// <summary>The pet this avatar is; null for a user.</summary>
        public RoomPet? Pet { get; init; }

        /// <summary>The pet this user rides, and the user riding this pet.</summary>
        public RoomEntity? Mount { get; set; }
        public RoomEntity? Rider { get; set; }

        /// <summary>An outfit worn in this room only, over the player's own look: a changing gate's.</summary>
        public string? RoomOutfit { get; set; }

        /// <summary>The chat bubbles asked about, whether the user may chat in each, and when that was asked.</summary>
        public Dictionary<int, (bool Allowed, DateTimeOffset CheckedAt)> ChatStyles { get; } = [];

        /// <summary>The look the room shows.</summary>
        public string Figure => RoomOutfit ?? Player.Figure;

        /// <summary>In a trade: the avatar shows it.</summary>
        public bool Trading { get; set; }

        public required Guid SessionGuid { get; set; }
        public required int RoomIndex { get; init; }
        public required int X { get; set; }
        public required int Y { get; set; }
        public required double Z { get; set; }
        public required int HeadDirection { get; set; }
        public required int BodyDirection { get; set; }
        public Queue<(int X, int Y)> Path { get; set; } = new();

        /// <summary>Where the walk is headed, kept so it can be routed again when somebody
        /// steps into the way. Null once the walk is over.</summary>
        public (int X, int Y)? Goal { get; set; }
        public bool Walking { get; set; }
        public bool Sitting { get; set; }
        public bool Laying { get; set; }

        /// <summary>How many of the status updates a roller's slide is followed by are still to come: Habbo tells
        /// where the rider stands on each of the two ticks after the slide.</summary>
        public int RolledUpdatesLeft { get; set; }

        /// <summary>The dance it is doing; 0 when it is not dancing.</summary>
        public int DanceStyle { get; set; }

        /// <summary>The sign it is holding up, and for how many more ticks.</summary>
        public int? Sign { get; set; }
        public int SignTicksLeft { get; set; }

        public bool Asleep { get; set; }

        /// <summary>The effect it is wearing; 0 for none.</summary>
        public int EffectId { get; set; }

        /// <summary>The group the user shows beside their name, if any.</summary>
        public FavouriteGroup? FavouriteGroup { get; set; }

        /// <summary>The groups the user is a member of, which group gates let through.</summary>
        public IReadOnlySet<int> GroupIds { get; set; } = new HashSet<int>();

        /// <summary>When the user came into the room, for the minutes spent in it.</summary>
        public DateTimeOffset EnteredAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>Tiles walked here not yet counted towards the walking achievement.</summary>
        public int TilesWalkedUncounted { get; set; }

        /// <summary>Whether the user has used a furni here yet, which the explorer achievement asks once.</summary>
        public bool UsedFurni { get; set; }

        /// <summary>The badges the user wears, in their slots' order.</summary>
        public IReadOnlyList<string> WornBadges { get; set; } = [];

        /// <summary>What the avatar carries in its hand (a drink, a snack); 0 is nothing.</summary>
        public int HandItem { get; set; }

        /// <summary>What to do when the current walk ends, such as using the machine it walked to.
        /// A new walk the player starts drops it.</summary>
        public Func<Task>? OnArrival { get; set; }

        /// <summary>How much control it has in the room (<see cref="RoomControllerLevels"/>).</summary>
        public int ControllerLevel { get; set; }

        /// <summary>The team it plays in (1 to 4); 0 for none.</summary>
        public int Team { get; set; }

        /// <summary>Frozen by wired: it cannot walk. A teleport unfreezes it, if the freeze said so.</summary>
        public bool Frozen { get; set; }
        public bool UnfreezeOnTeleport { get; set; }

        /// <summary>Muted by wired until then: what it says is not heard.</summary>
        public DateTime MutedUntil { get; set; }

        /// <summary>Ticks since it last did anything.</summary>
        public int IdleTicks { get; set; }

        /// <summary>Invisible staff (:invisible): nobody else is shown them.</summary>
        public bool Invisible { get; set; }

        /// <summary>Staff walking by teleport (:teleport): a click takes them straight to the tile.</summary>
        public bool Teleporting { get; set; }

        /// <summary>The user's rank among those with the most badges, as of entering (-1 for none).</summary>
        public int BadgesRank { get; set; } = -1;

        /// <summary>The last action it performed (a wired user action code), with its extra — which
        /// sign, which dance — and the tick it happened on.</summary>
        public int? LastAction { get; set; }
        public string LastActionExtra { get; set; } = string.Empty;
        public int LastActionTick { get; set; }
    }
}
