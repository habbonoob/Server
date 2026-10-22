using Habbo.Domain.Pets;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Pets;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Pets;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

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
            pet.Commands.Select(c => c.RequiredLevel).Distinct().Order().ToList(),
            data.AnyoneCanRide ? AnyoneRides : OwnerRides, pet.CanBreed, pet.CanHarvest, pet.CanRevive, data.Rarity,
            plant ? rules.WellBeingSeconds : 0, plant ? RemainingWellSeconds(data, now) : 0, plant ? RemainingGrowingSeconds(data, now) : 0,
            data.BreedingAllowed));
        return Task.CompletedTask;
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

            var now = DateTimeOffset.UtcNow;
            if (petRules.Energy(pet.Data, now) < command.EnergyCost)
                continue;

            if (!await ObeyAsync(entity, command, speaker))
                continue;

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
                if (!store.ClearPosture(entity))
                    return false;
                Broadcast(UserUpdateMessageComposer.From([store.CurrentStatus(entity)]));
                return true;

            case PetCommandAction.Forward:
                var step = RoomEntityStore.Offset(entity.BodyDirection);
                if (store.Slide(entity, entity.X + step.X, entity.Y + step.Y) is not { } moved)
                    return false;
                Broadcast(UserUpdateMessageComposer.From([moved]));
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

            default:
                return false;
        }
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
