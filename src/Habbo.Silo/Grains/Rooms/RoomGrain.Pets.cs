using Habbo.Domain.Pets;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Pets;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Pets;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's pets: putting them down and picking them up, what they are told and do, how they
/// learn, and wandering about when left to themselves.</summary>
public sealed partial class RoomGrain
{
    private const string PetCommandTextPrefix = "pet.command.";

    /// <summary>What a turn left or right moves a pet round by: a quarter of the eight directions.</summary>
    private const int QuarterTurn = 2;
    private const int Directions = 8;

    /// <summary>Every command pets learn; each pet knows those its kind can show.</summary>
    private List<PetCommand> _petCommands = [];

    private readonly Random _petRandom = new();

    // what habbo.com's level 10 dog did when trained (25 October 2026)
    private const int PetSmileSeconds = 10;
    private const string StandPosture = "std";
    private const int StandSeconds = 4;
    private const string RefusingPosture = "sit";
    private const int RefusingSeconds = 4;

    private async Task LoadPetsAsync(AppDbContext db)
    {
        _petCommands = await db.PetCommands.AsNoTracking().OrderBy(c => c.RequiredLevel).ThenBy(c => c.Id).ToListAsync();
        var pets = await db.Pets.AsNoTracking().Include(p => p.Type).ThenInclude(t => t!.Palettes).Include(p => p.Owner)
            .Where(p => p.RoomId == _room!.Id).ToListAsync();
        foreach (var pet in pets)
        {
            var (x, y) = OnFloor(pet.X, pet.Y);
            AddPet(pet, pet.Owner!.AccountId, pet.Owner.Username, x, y, pet.Rotation);
        }
    }

    /// <summary>The commands a kind of pet can be taught: those whose posture it can take, and those that need none.</summary>
    private List<PetCommand> CommandsFor(PetType type) =>
        _petCommands.Where(c => c.Posture is null || type.Postures.Contains(c.Posture)).ToList();

    private RoomEntityStore.RoomEntity AddPet(Pet pet, int ownerAccountId, string ownerName, int x, int y, int direction)
    {
        // a plant knows no commands
        var roomPet = new RoomPet
        {
            Data = pet, OwnerAccountId = ownerAccountId, OwnerName = ownerName,
            Commands = IsPlant(pet) ? [] : CommandsFor(pet.Type!),
        };
        if (IsPlant(pet))
            RefreshPlant(roomPet);
        var entity = _entities!.AddPet(roomPet, PetFigureWireData.FigureString(pet), x, y, _items!.AvatarHeightAt(x, y), direction);
        roomPet.NextWanderTick = _entities.CurrentTick + WanderPauseTicks;
        return entity;
    }

    private int WanderPauseTicks => petOptions.Value.WanderPauseSeconds * TicksPerSecond;

    public async Task PlacePetAsync(Guid playerId, int petId, int x, int y)
    {
        if (_room is null || _model is null || _items is null || _entities?.Find(playerId) is not { } placer)
            return;

        var rules = petOptions.Value;
        var refusal = !rules.Enabled ? PetPlacingErrorComposer.ForbiddenInHotel
            : _room.OwnerId != playerId && !_room.AllowPets ? PetPlacingErrorComposer.ForbiddenInFlat
            : _entities.Pets.Count >= rules.MaxPerRoom ? PetPlacingErrorComposer.MaxPets
            : _entities.Pets.Count(p => p.Pet!.Data.OwnerId == playerId) >= rules.MaxOwnPerRoom ? PetPlacingErrorComposer.MaxOwnPets
            : !_model.IsTile(x, y) || _items.BlocksWalking(x, y) || _entities.HasAvatarAt(x, y) ? PetPlacingErrorComposer.TileNotFree
            : (int?)null;
        if (refusal is { } error)
        {
            SendTo(playerId, new PetPlacingErrorComposer(error));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Pets.Include(p => p.Type).ThenInclude(t => t!.Palettes).FirstOrDefaultAsync(p => p.Id == petId && p.OwnerId == playerId && p.RoomId == null) is not { } row)
            return;

        row.RoomId = _room.Id;
        row.X = x;
        row.Y = y;
        await db.SaveChangesAsync();

        var entity = AddPet(row, placer.Player.AccountId, placer.Player.Username, x, y, row.Rotation);
        Broadcast(UsersMessageComposer.From([_entities.ToWireData(entity)]));
        Broadcast(UserUpdateMessageComposer.From([_entities.LookTo(entity, placer.X, placer.Y) ?? _entities.CurrentStatus(entity)]));
        SendTo(playerId, new PetRemovedFromInventoryComposer(petId));
        if (playerId != _room.OwnerId)
            TrackAction(playerId, Habbo.Silo.RewardTracks.RewardTrackActions.PetOtherUsersRoom);
    }

    public async Task RemovePetAsync(Guid playerId, int petId)
    {
        if (_room is null || _entities?.FindPet(petId) is not { Pet: { } pet } entity
            || (pet.Data.OwnerId != playerId && _room.OwnerId != playerId))
            return;

        await ReturnPetAsync(entity);
    }

    /// <summary>Takes a pet out of the room and back into its owner's inventory.</summary>
    private async Task ReturnPetAsync(RoomEntityStore.RoomEntity entity)
    {
        var petId = entity.Pet!.Data.Id;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Pets.FirstOrDefaultAsync(p => p.Id == petId) is not { } row)
            return;

        row.RoomId = null;
        row.Rotation = entity.BodyDirection;
        await db.SaveChangesAsync();

        if (entity.Rider is { } rider)
            Dismount(rider);
        LeaveBreedingNest(entity);
        _entities.RemovePet(petId);
        Broadcast(new UserRemoveMessageComposer(entity.RoomIndex));

        // it goes back to whoever owns it, wherever they are
        var returned = new PetAddedToInventoryComposer(PetData.From(row), OpenInventory: false);
        await GrainFactory.GetGrain<IPlayerGrain>(row.OwnerId).SendAsync(returned.ToMessage());
    }

    public async Task MovePetAsync(Guid playerId, int petId, int x, int y, int direction)
    {
        if (_room is null || _model is null || _items is null || _entities?.FindPet(petId) is not { Pet: { } pet } entity
            || (pet.Data.OwnerId != playerId && _room.OwnerId != playerId) || entity.Rider is not null
            || !_model.IsTile(x, y) || _items.BlocksWalking(x, y) || (_entities.HasAvatarAt(x, y) && (entity.X != x || entity.Y != y)))
            return;

        // moved off whatever it was doing: out of a nest, and off a walk to food or a bed whose arrival would have freed it
        LeaveBreedingNest(entity);
        if (entity.OnArrival is not null)
        {
            entity.OnArrival = null;
            pet.Busy = false;
        }

        _entities.ClearPosture(entity);
        _entities.Warp(entity, x, y);
        Broadcast(UserUpdateMessageComposer.From([_entities.Turn(entity, ((direction % Directions) + Directions) % Directions)]));
        await SavePetAsync(pet, row =>
        {
            row.X = x;
            row.Y = y;
            row.Rotation = entity.BodyDirection;
        });
    }

    public Task SendPetInfoAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { } pet } entity)
            return Task.CompletedTask;

        // a pet found and looked at
        if (_entities.Find(playerId) is { } viewer)
            TrackAction(viewer, Habbo.Silo.RewardTracks.RewardTrackActions.FindPet, pet.Type.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var now = DateTimeOffset.UtcNow;
        var data = pet.Data;
        // a plant's level is how far it has grown, and it is well for as long as it was last watered for
        var plant = IsPlant(data);
        if (plant)
            RefreshPlant(pet);
        var rules = plantOptions.Value;
        SendTo(playerId, new PetInfoMessageComposer(
            data.Id, data.Name, data.Level, plant ? GrownLevel(pet.Type) : petRules.MaxLevel, data.Experience, petRules.ExperienceForNextLevel(data.Level),
            petRules.Energy(data, now), petRules.MaxEnergy(data.Level), petRules.Nutrition(data, now), petRules.MaxNutrition(data.Level),
            data.Respect, pet.OwnerAccountId, PetRules.AgeInDays(data, now), pet.OwnerName, data.PaletteId,
            HasFreeSaddle: data.SaddleDefinitionId is not null && entity.Rider is null, IsRiding: entity.Rider is not null,
            // the client draws a special skill from these with one image per threshold passed, and has only five;
            // habbo.com sends a dog none
            SkillThresholds: [],
            data.AnyoneCanRide ? AnyoneRides : OwnerRides, pet.CanBreed, pet.CanHarvest, pet.CanRevive, data.Rarity,
            plant ? rules.WellBeingSeconds : 0, plant ? RemainingWellSeconds(data, now) : 0, plant ? RemainingGrowingSeconds(data, now) : 0,
            data.BreedingAllowed));
        return Task.CompletedTask;
    }

    /// <summary>What wired's ~pet and ~plant variables read of a pet: what its info card shows.</summary>
    private Habbo.Silo.Wired.WiredPetStats? WiredPetStatsOf(RoomEntityStore.RoomEntity entity)
    {
        if (entity.Pet is not { } pet)
            return null;

        var now = DateTimeOffset.UtcNow;
        var data = pet.Data;
        var plant = IsPlant(data);
        if (plant)
            RefreshPlant(pet);
        var stats = new Habbo.Silo.Wired.WiredPetStats(data.Level, plant ? GrownLevel(pet.Type) : petRules.MaxLevel, data.Experience,
            petRules.ExperienceForNextLevel(data.Level), petRules.Energy(data, now), petRules.MaxEnergy(data.Level),
            petRules.Nutrition(data, now), petRules.MaxNutrition(data.Level), data.Respect, pet.OwnerAccountId, data.CreatedAt, data.Rarity);
        if (!plant)
            return stats;

        var growing = RemainingGrowingSeconds(data, now);
        return stats with
        {
            Plant = new Habbo.Silo.Wired.WiredPlantStats(pet.CanBreed, pet.CanHarvest, pet.CanRevive, IsDead: data.DiedAt is not null,
                IsGrowing: data.DiedAt is null && growing > 0, plantOptions.Value.WellBeingSeconds, RemainingWellSeconds(data, now), growing,
                // its colour is its palette, its shape the part on its body layer
                Color: data.PaletteId, Shape: Habbo.Silo.Pets.PlantLooks.PartOn(pet.Type, data.CustomParts, Habbo.Silo.Pets.PlantLooks.BodyTag)),
        };
    }

    /// <summary>A pet picked out turns to look at whoever picked it.</summary>
    public Task PetSelectedAsync(Guid playerId, int petId)
    {
        if (_entities?.Find(playerId) is { } selector && _entities.FindPet(petId) is { } entity && !entity.Walking)
            LookAt(entity, selector.X, selector.Y);
        return Task.CompletedTask;
    }

    public Task SendPetCommandsAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is { Pet: { } pet })
            SendTo(playerId, new PetCommandsMessageComposer(petId, pet.Commands.Select(c => c.Id).ToList(), pet.Obeyed.Select(c => c.Id).ToList()));
        return Task.CompletedTask;
    }

    public async Task RespectPetAsync(Guid playerId, int petId)
    {
        if (_entities?.Find(playerId) is not { } giver || _entities.FindPet(petId) is not { Pet: { } pet } entity)
            return;

        var rules = petOptions.Value;
        var accountDays = (int)(DateTimeOffset.UtcNow - giver.Player.CreatedAt).TotalDays;
        if (accountDays < rules.RespectRequiredAccountDays)
        {
            SendTo(playerId, new PetRespectFailedComposer(rules.RespectRequiredAccountDays, accountDays));
            return;
        }

        if (!await GrainFactory.GetGrain<IPlayerGrain>(playerId).SpendPetRespectAsync())
            return;

        await SavePetAsync(pet, row => row.Respect++);
        // scratching another Habbo's pet: one given, and one their pet got
        if (playerId != pet.Data.OwnerId)
        {
            foreach (var achievement in PetAchievements.RespectGiver(pet.Data.TypeId))
                GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(achievement, 1).Ignore();
            GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(AchievementNames.PetRespectReceiver, 1).Ignore();
        }
        Broadcast(new PetRespectNotificationComposer(pet.Data.Respect, pet.OwnerAccountId, PetData.From(pet.Data)));
        LookAt(entity, giver.X, giver.Y);
        // scratched by its owner, it wags its tail at them
        if (playerId == pet.Data.OwnerId && !IsPlant(pet.Data))
            await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Wag);
        await PetGainsExperienceAsync(entity, rules.RespectExperience);
    }

    /// <summary>What was said to a pet by its owner: "name command", the command as the client names it.
    /// A pet obeys a command it knows at its level when it has the energy, and learns from it.</summary>
    private async Task PetsHearAsync(RoomEntityStore.RoomEntity speaker, string text)
    {
        if (_entities is null)
            return;

        foreach (var entity in _entities.Pets.Where(p => p.Pet!.Data.OwnerId == speaker.Player.Id).ToList())
        {
            var pet = entity.Pet!;
            var name = pet.Data.Name + " ";
            if (!text.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                continue;

            var said = text[name.Length..].Trim();
            PetCommand? command = null;
            foreach (var known in pet.Obeyed)
            {
                if (string.Equals(await gameTexts.GetAsync(PetCommandTextPrefix + known.Id), said, StringComparison.OrdinalIgnoreCase))
                    command = known;
            }

            if (command is null)
                continue;

            // now and then it will not (habbo.com: whatever its energy, though never its food), and it will not do what
            // it has not the energy for, asking for food instead
            var now = DateTimeOffset.UtcNow;
            if (command.Action is not (PetCommandAction.Eat or PetCommandAction.Free or PetCommandAction.Breed)
                && _petRandom.NextDouble() < await PetSpeech.RefuseChanceAsync())
            {
                await RefuseAsync(entity, speaker);
                continue;
            }

            if (petRules.Energy(pet.Data, now) < command.EnergyCost)
            {
                await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Hungry);
                continue;
            }

            // pleased with itself, it smiles a while (habbo.com: from the command on, walking or not)
            var smiledUntil = pet.SmileUntilTick;
            pet.SmileUntilTick = _entities.CurrentTick + PetSmileSeconds * TicksPerSecond;
            if (!await ObeyAsync(entity, command, speaker))
            {
                pet.SmileUntilTick = smiledUntil;
                continue;
            }

            await SavePetAsync(pet, row =>
            {
                petRules.Settle(row, now);
                row.Energy -= command.EnergyCost;
            });
            await PetGainsExperienceAsync(entity, command.Experience);
        }
    }

    /// <summary>Does what the command says; false when there was nothing it could do.</summary>
    private async Task<bool> ObeyAsync(RoomEntityStore.RoomEntity entity, PetCommand command, RoomEntityStore.RoomEntity speaker)
    {
        if (entity.Rider is not null || entity.Pet!.Busy)
            return false;

        var store = _entities!;
        var pet = entity.Pet!;
        switch (command.Action)
        {
            case PetCommandAction.Posture when command.Posture is { } posture:
                if (entity.Walking)
                    store.Warp(entity, entity.X, entity.Y);
                pet.Posture = posture;
                pet.PostureUntilTick = command.PostureSeconds > 0 ? store.CurrentTick + command.PostureSeconds * TicksPerSecond : 0;
                Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
                // playing or wagging its tail, it says so
                if (Habbo.Silo.Pets.PetSpeech.ForPosture(posture) is { } kind)
                    await PetSaysAsync(entity, kind);
                return true;

            case PetCommandAction.Free:
                pet.Staying = false;
                pet.Following = null;
                pet.NextWanderTick = store.CurrentTick;
                if (store.ClearPosture(entity))
                    Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
                return true;

            case PetCommandAction.Stay:
                pet.Staying = true;
                pet.Following = null;
                if (entity.Walking)
                    Broadcast(UserUpdateMessageComposer.From([store.Warp(entity, entity.X, entity.Y)]));
                return true;

            case PetCommandAction.Come:
                pet.Following = null;
                var (x, y) = NearestFreeNeighbour(speaker, entity);
                store.Walk(entity, x, y);
                return true;

            case PetCommandAction.Follow:
                pet.Following = speaker.Player.Id;
                pet.Staying = false;
                return true;

            case PetCommandAction.Stand:
                // habbo.com's dog stands up (std) a few seconds; a kind with no such posture just stops what it held
                if (entity.Walking)
                    store.Warp(entity, entity.X, entity.Y);
                if (pet.Type.Postures.Contains(StandPosture))
                {
                    pet.Posture = StandPosture;
                    pet.PostureUntilTick = store.CurrentTick + StandSeconds * TicksPerSecond;
                }
                else if (!store.ClearPosture(entity))
                {
                    return false;
                }
                Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
                return true;

            case PetCommandAction.Speak:
                if (store.ClearPosture(entity))
                    Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
                await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Speak);
                return true;

            case PetCommandAction.Forward:
                var step = RoomEntityStore.Offset(entity.BodyDirection);
                if (store.Slide(entity, entity.X + step.X, entity.Y + step.Y) is not { } moved)
                    return false;
                BroadcastStep(entity, moved);
                return true;

            case PetCommandAction.TurnLeft:
            case PetCommandAction.TurnRight:
                var turn = command.Action == PetCommandAction.TurnLeft ? Directions - QuarterTurn : QuarterTurn;
                Broadcast(UserUpdateMessageComposer.From([store.Turn(entity, (entity.BodyDirection + turn) % Directions)]));
                return true;

            case PetCommandAction.Eat:
                return await SeekCareAsync(entity, PetCare.Eat);

            case PetCommandAction.Drink:
                return await SeekCareAsync(entity, PetCare.Drink);

            case PetCommandAction.Nest:
                return await SeekCareAsync(entity, PetCare.Rest);

            case PetCommandAction.Breed:
                return await GoToBreedingNestAsync(entity, speaker);

            case PetCommandAction.Football:
                return await PlayFootballAsync(entity);

            default:
                return false;
        }
    }

    /// <summary>Play football (pet.command.17): the pet runs onto the nearest football, and its walk ending on the ball
    /// kicks it ahead the way it came, as a user's does (<see cref="FootballPitch"/>); standing on one, it steps off it,
    /// heeling it back. Kicking, it plays: a play line. False when the room has no football.</summary>
    private async Task<bool> PlayFootballAsync(RoomEntityStore.RoomEntity entity)
    {
        var store = _entities!;
        if (_items?.Items.Where(FootballPitch.IsBall)
                .OrderBy(item => Math.Max(Math.Abs(item.X - entity.X), Math.Abs(item.Y - entity.Y)))
                .FirstOrDefault() is not { } ball)
            return false;

        entity.Pet!.Following = null;
        if (entity.X == ball.X && entity.Y == ball.Y)
        {
            var off = Enumerable.Range(0, Directions)
                .Select(RoomEntityStore.Offset)
                .Select(offset => (X: entity.X + offset.X, Y: entity.Y + offset.Y))
                .FirstOrDefault(tile => _model!.IsTile(tile.X, tile.Y) && !_items.BlocksWalking(tile.X, tile.Y) && !store.HasAvatarAt(tile.X, tile.Y),
                    (entity.X, entity.Y));
            if (!store.Walk(entity, off.X, off.Y))
                return false;
            await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Play);
            return true;
        }

        await WalkThenAsync(entity, ball.X, ball.Y, async () =>
        {
            // there as the ball is: the step onto it kicks it
            if (store.FindPet(entity.Pet.Data.Id) == entity && _items.FloorItemsOn(entity.X, entity.Y).Any(FootballPitch.IsBall))
                await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Play);
        });
        return entity.Walking;
    }

    /// <summary>The owner's pets glad to see them come in (habbo.com: "*Yay! You're home!*" as the room opens).</summary>
    private async Task PetsGreetOwnerAsync(RoomEntityStore.RoomEntity owner)
    {
        foreach (var entity in _entities?.Pets.Where(p => p.Pet!.Data.OwnerId == owner.Player.Id && !IsPlant(p.Pet.Data)).ToList() ?? [])
            await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Greet);
    }

    /// <summary>A refused command, as habbo.com's dog refuses one: it turns its back on whoever asked, sits a while, and
    /// says so.</summary>
    private async Task RefuseAsync(RoomEntityStore.RoomEntity entity, RoomEntityStore.RoomEntity speaker)
    {
        var store = _entities!;
        var pet = entity.Pet!;
        if (entity.Rider is not null || pet.Busy)
            return;

        if (entity.Walking)
            store.Warp(entity, entity.X, entity.Y);
        store.LookTo(entity, 2 * entity.X - speaker.X, 2 * entity.Y - speaker.Y);
        if (pet.Type.Postures.Contains(RefusingPosture))
        {
            pet.Posture = RefusingPosture;
            pet.PostureUntilTick = store.CurrentTick + RefusingSeconds * TicksPerSecond;
        }
        Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
        await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.Refuse);
    }

    private Habbo.Silo.Pets.PetSpeech PetSpeech => ServiceProvider.GetRequiredService<Habbo.Silo.Pets.PetSpeech>();

    /// <summary>A line of the kind the pet says, if pets have any (seed/pet_speech.json).</summary>
    private async Task PetSaysAsync(RoomEntityStore.RoomEntity entity, string kind)
    {
        if (await PetSpeech.LineAsync(kind, _petRandom, entity.Pet!.OwnerName) is { } line)
            PetSays(entity, line);
    }

    /// <summary>A line the pet says, heard by everyone in the room, as habbo.com's pets say theirs: in no style, with
    /// no gesture.</summary>
    private void PetSays(RoomEntityStore.RoomEntity entity, string text)
    {
        if (_entities is null)
            return;
        SendChat(new ChatMessage(entity.RoomIndex, text, Gesture.None, StyleId: 0, Links: [], Habbo.Protocol.Messages.Outgoing.Room.Chat.ChatMessageComposer.Untracked),
            RoomChatKind.Talk, _entities.Entities, toSpectators: true);
    }

    /// <summary>Adds experience, and a level when it is enough for one: the room sees the pet grow, and
    /// its owner is told wherever they are.</summary>
    private async Task PetGainsExperienceAsync(RoomEntityStore.RoomEntity entity, int experience)
    {
        var pet = entity.Pet!;
        if (experience <= 0 || pet.Data.Level >= petRules.MaxLevel)
            return;

        var before = pet.Data.Level;
        await SavePetAsync(pet, row =>
        {
            row.Experience += experience;
            row.Level = Math.Min(petRules.LevelFor(row.Experience), petRules.MaxLevel);
        });
        Broadcast(new PetExperienceComposer(pet.Data.Id, entity.RoomIndex, experience));
        if (pet.Data.Level == before)
            return;

        Broadcast(new PetLevelUpdateComposer(entity.RoomIndex, pet.Data.Id, pet.Data.Level));
        await PetSaysAsync(entity, Habbo.Silo.Pets.PetSpeech.LevelUp);
        foreach (var achievement in PetAchievements.LevelUp(pet.Data.TypeId))
            GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(achievement, pet.Data.Level - before).Ignore();
        var notice = new PetLevelNotificationComposer(pet.Data.Id, pet.Data.Name, pet.Data.Level, PetFigureWireData.From(pet.Data));
        await GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).SendAsync(notice.ToMessage());
    }

    /// <summary>Changes the pet's row, and the room's copy of it the same way.</summary>
    private async Task SavePetAsync(RoomPet pet, Action<Pet> change)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Pets.FirstOrDefaultAsync(p => p.Id == pet.Data.Id) is not { } row)
            return;

        change(row);
        change(pet.Data);
        await db.SaveChangesAsync();
    }

    /// <summary>What pets do by themselves each tick: keep up with whoever they follow, and wander about
    /// when nothing holds them.</summary>
    private async Task PetsActAsync()
    {
        if (_entities is null)
            return;

        var tick = _entities.CurrentTick;
        foreach (var entity in _entities.Pets.ToList())
        {
            var pet = entity.Pet!;
            if (IsPlant(pet.Data))
            {
                await TendPlantAsync(entity);
                continue;
            }

            if (pet.Following is { } followed)
            {
                if (_entities.Find(followed) is not { } leader)
                {
                    pet.Following = null;
                    continue;
                }

                if (!entity.Walking && (Math.Abs(leader.X - entity.X) > 1 || Math.Abs(leader.Y - entity.Y) > 1))
                {
                    var (x, y) = NearestFreeNeighbour(leader, entity);
                    _entities.Walk(entity, x, y);
                }

                continue;
            }

            if (pet.Busy || entity.Rider is not null || pet.Staying || pet.Posture is not null || entity.Walking || tick < pet.NextWanderTick)
                continue;

            pet.NextWanderTick = tick + WanderPauseTicks;

            // hungry or tired, it sees to that before anything else
            if (await SeeToNeedsAsync(entity))
                continue;

            if (_entities.RandomReachableTile(entity, _petRandom) is { } tile)
                _entities.Walk(entity, tile.X, tile.Y);
        }
    }
}
