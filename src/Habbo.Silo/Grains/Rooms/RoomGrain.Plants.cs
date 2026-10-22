using System.Text.RegularExpressions;
using Habbo.Domain.Furniture;
using Habbo.Domain.Pets;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Pets;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Pets;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Monster plants: planted from seeds, growing through their stages, well while watered and dead
/// when not, sped up by light and fertiliser, revived, bred once (with the other owner's say) into seeds,
/// harvested when grown and composted when dead.</summary>
public sealed partial class RoomGrain
{
    private const int WaterSupplement = 0;
    private const int LightSupplement = 1;

    private const string GrowingPosture = "grw";
    private const string IllSuffix = "ill";
    private const string IllPosture = "ill";
    private const string DeadPosture = "rip";
    private const string PlantColor = "FFFFFF";

    /// <summary>The growing stage postures the plant asset has: grw1, grw2, ...</summary>
    private static readonly Regex GrowingStage = new(@"^grw(\d+)$");

    /// <summary>Requests to breed with another owner's plant, by the two plants, and who asked.</summary>
    private readonly Dictionary<(int Asking, int Asked), Guid> _plantRequests = [];

    private bool IsPlant(Pet pet) => pet.TypeId == plantOptions.Value.TypeId;

    /// <summary>The level a plant is grown at: one past its last growing stage.</summary>
    private static int GrownLevel(PetType type) =>
        type.Postures.Select(p => GrowingStage.Match(p)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).DefaultIfEmpty(0).Max() + 1;

    private double GrowingElapsedSeconds(Pet pet, DateTimeOffset now) =>
        (now - pet.CreatedAt).TotalSeconds + pet.GrowthSkippedSeconds;

    private int RemainingGrowingSeconds(Pet pet, DateTimeOffset now) =>
        (int)Math.Max(0, plantOptions.Value.GrowSeconds - GrowingElapsedSeconds(pet, now));

    /// <summary>The stage the plant has grown to: its growing time shared out over its stages.</summary>
    private int PlantLevel(Pet pet, DateTimeOffset now)
    {
        var grown = GrownLevel(pet.Type!);
        var rules = plantOptions.Value;
        if (rules.GrowSeconds <= 0 || grown <= 1)
            return grown;

        var perStage = (double)rules.GrowSeconds / (grown - 1);
        return Math.Min(grown, 1 + (int)(GrowingElapsedSeconds(pet, now) / perStage));
    }

    private int RemainingWellSeconds(Pet pet, DateTimeOffset now) =>
        pet.WellUntil is { } until ? (int)Math.Max(0, (until - now).TotalSeconds) : 0;

    /// <summary>Brings the room's view of the plant up to date — how it stands and what can be done with it.
    /// True when anything changed.</summary>
    private bool RefreshPlant(RoomPet pet)
    {
        var data = pet.Data;
        var now = DateTimeOffset.UtcNow;
        var grown = PlantLevel(data, now) >= GrownLevel(pet.Type);
        var alive = data.DiedAt is null;
        var ill = alive && RemainingWellSeconds(data, now) * WholeChance < plantOptions.Value.WellBeingSeconds * plantOptions.Value.IllBelowPercent;

        string? posture;
        if (!alive)
            posture = DeadPosture;
        else if (!grown)
        {
            var stage = GrowingPosture + PlantLevel(data, now);
            posture = ill && pet.Type.Postures.Contains(stage + IllSuffix) ? stage + IllSuffix : stage;
        }
        else
            posture = ill && pet.Type.Postures.Contains(IllPosture) ? IllPosture : null;

        if (posture is not null && !pet.Type.Postures.Contains(posture))
            posture = null;

        var before = (pet.Posture, pet.CanBreed, pet.CanHarvest, pet.CanRevive);
        pet.Posture = posture;
        pet.PostureUntilTick = 0;
        pet.CanBreed = alive && grown && data.CanBreed;
        pet.CanHarvest = alive && grown;
        pet.CanRevive = !alive;
        return before != (pet.Posture, pet.CanBreed, pet.CanHarvest, pet.CanRevive);
    }

    /// <summary>Shows the room how the plant stands and what can be done with it.</summary>
    private void ShowPlant(RoomEntityStore.RoomEntity entity)
    {
        var pet = entity.Pet!;
        Broadcast(UserUpdateMessageComposer.From([_entities!.CurrentStatus(entity)]));
        Broadcast(new PetStatusUpdateComposer(entity.RoomIndex, pet.Data.Id, pet.CanBreed, pet.CanHarvest, pet.CanRevive, pet.Data.BreedingAllowed));
    }

    /// <summary>A plant as time passes: it grows a stage, or dies when it has gone too long without water.</summary>
    private async Task TendPlantAsync(RoomEntityStore.RoomEntity entity)
    {
        var pet = entity.Pet!;
        var now = DateTimeOffset.UtcNow;
        if (pet.Data.DiedAt is null && pet.Data.WellUntil is { } until && until <= now)
        {
            await SavePetAsync(pet, data => data.DiedAt = now);
            GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(AchievementNames.MonsterPlantGardenOfDeath, 1).Ignore();
        }

        var level = PlantLevel(pet.Data, now);
        if (level != pet.Data.Level)
        {
            await SavePetAsync(pet, data => data.Level = level);
            Broadcast(new PetLevelUpdateComposer(entity.RoomIndex, pet.Data.Id, level));
        }

        if (RefreshPlant(pet))
            ShowPlant(entity);
    }

    /// <summary>The owner uses a seed: it is gone, and a plant grows where it lay — the one two plants bred
    /// into it, or a new one at least as rare as the seed says. False when the item is not a seed.</summary>
    private async Task<bool> PlantSeedAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not MonsterPlantSeedInteraction)
            return false;
        if (_room is null || item.Item.OwnerId != user.Player.Id || item.Temporary)
            return true;

        var rules = plantOptions.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var type = await db.PetTypes.AsNoTracking().Include(t => t.Palettes).Include(t => t.Layers).FirstOrDefaultAsync(t => t.Id == rules.TypeId);
        if (type is null)
            return true;

        var bred = await db.PlantSeeds.AsNoTracking().FirstOrDefaultAsync(s => s.ItemId == item.Item.Id);
        var minimumRarity = int.TryParse(item.Item.Definition!.CustomParams, out var least) ? least : 0;
        var look = bred is not null
            ? new PlantLook(bred.PaletteId, bred.CustomParts, bred.Rarity)
            : await plantLooks.NewAsync(type, minimumRarity, _petRandom);
        if (look is null)
            return true;

        var (x, y, rotation) = (item.X, item.Y, item.Rotation);
        if (!await _furniture!.RemoveAsync(item))
            return true;

        var now = DateTimeOffset.UtcNow;
        var plant = new Pet
        {
            OwnerId = user.Player.Id, TypeId = type.Id, PaletteId = look.PaletteId, CustomParts = look.CustomParts, Rarity = look.Rarity,
            Color = PlantColor, Name = await gameTexts.GetAsync(rules.NameTextKey) ?? type.Code,
            RoomId = _room.Id, X = x, Y = y, Rotation = rotation, CreatedAt = now, WellUntil = now.AddSeconds(rules.WellBeingSeconds),
        };
        petRules.Start(plant, now);
        db.Pets.Add(plant);
        await db.SaveChangesAsync();
        plant.Type = type;

        var entity = AddPet(plant, user.Player.AccountId, user.Player.Username, x, y, rotation);
        Broadcast(UsersMessageComposer.From([_entities!.ToWireData(entity)]));
        ShowPlant(entity);
        GrainFactory.GetGrain<IPlayerGrain>(user.Player.Id).CountPetsAsync().Ignore();
        return true;
    }

    /// <summary>When each player last gave each plant light.</summary>
    private readonly Dictionary<(Guid Player, int Plant), DateTimeOffset> _lightGiven = [];

    /// <summary>Anyone may water a living plant, which keeps it well, or give it light, which spares it some growing.</summary>
    public async Task GiveSupplementAsync(Guid playerId, int petId, int supplementType)
    {
        if (_entities?.Find(playerId) is not { } giver || _entities.FindPet(petId) is not { Pet: { } pet } entity
            || !IsPlant(pet.Data) || pet.Data.DiedAt is not null || supplementType is not (WaterSupplement or LightSupplement))
            return;

        var rules = plantOptions.Value;
        // light spares growing once per giver and plant for as long as it spares, so one visitor sending it over and over
        // cannot grow a plant in seconds; past full grown it spares nothing
        if (supplementType == LightSupplement)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lightGiven.TryGetValue((playerId, petId), out var last) && now - last < TimeSpan.FromSeconds(rules.LightGrowthSeconds))
                return;
            _lightGiven[(playerId, petId)] = now;
        }

        await SavePetAsync(pet, data =>
        {
            if (supplementType == WaterSupplement)
                data.WellUntil = DateTimeOffset.UtcNow.AddSeconds(rules.WellBeingSeconds);
            else
                data.GrowthSkippedSeconds = Math.Min(data.GrowthSkippedSeconds + rules.LightGrowthSeconds, rules.GrowSeconds);
        });

        Broadcast(new PetSupplementedNotificationComposer(petId, giver.Player.AccountId, supplementType));
        GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(AchievementNames.MonsterPlantTreater, 1).Ignore();
        await TendPlantAsync(entity);
    }

    /// <summary>A plant potion used on the owner's plant: revival for a dead one, rebreeding for one that
    /// has bred and is no rarer than the potion is for, fertiliser for one still growing. False when it does nothing.</summary>
    private async Task<bool> TreatPlantAsync(RoomEntityStore.RoomEntity entity, PetProductKind kind, string potion)
    {
        var pet = entity.Pet!;
        if (!IsPlant(pet.Data))
            return false;

        var rules = plantOptions.Value;
        var now = DateTimeOffset.UtcNow;
        var alive = pet.Data.DiedAt is null;
        var grown = PlantLevel(pet.Data, now) >= GrownLevel(pet.Type);
        switch (kind)
        {
            case PetProductKind.Revival when !alive:
                await SavePetAsync(pet, data =>
                {
                    data.DiedAt = null;
                    data.WellUntil = now.AddSeconds(rules.WellBeingSeconds);
                });
                GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(AchievementNames.MonsterPlantHealer, 1).Ignore();
                break;

            case PetProductKind.Rebreed when alive && !pet.Data.CanBreed
                                             && rules.RebreedMaxRarity.TryGetValue(potion, out var rarest) && pet.Data.Rarity <= rarest:
                await SavePetAsync(pet, data => data.CanBreed = true);
                break;

            case PetProductKind.Fertiliser when alive && !grown:
                await SavePetAsync(pet, data => data.GrowthSkippedSeconds += RemainingGrowingSeconds(data, now));
                break;

            default:
                return false;
        }

        await TendPlantAsync(entity);
        return true;
    }

    /// <summary>The owner harvests their grown plant: it is gone, and they have the plant as furniture.</summary>
    public async Task HarvestPlantAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { } pet } entity || pet.Data.OwnerId != playerId || !IsPlant(pet.Data))
            return;

        RefreshPlant(pet);
        if (!pet.CanHarvest || await ReplacePlantAsync(entity, plantOptions.Value.HarvestClassName) is not { } harvest)
            return;

        await GrainFactory.GetGrain<IPlayerGrain>(playerId).AnnounceItemsAsync([harvest.Id]);
    }

    /// <summary>The owner turns their dead plant into compost, which is left where the plant stood.</summary>
    public async Task CompostPlantAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { } pet } entity || pet.Data.OwnerId != playerId || !IsPlant(pet.Data)
            || pet.Data.DiedAt is null)
            return;

        var (x, y, rotation) = (entity.X, entity.Y, entity.BodyDirection);
        if (await ReplacePlantAsync(entity, plantOptions.Value.CompostClassName) is not { } compost)
            return;

        await PlaceFloorItemAsync(playerId, compost.Id, x, y, rotation);
        if (_items!.Find(compost.Id) is null)
            await GrainFactory.GetGrain<IPlayerGrain>(playerId).AnnounceItemsAsync([compost.Id]);
    }

    /// <summary>Takes the plant out of the room for good, and gives its owner the furniture it turned into.</summary>
    private async Task<UserItem?> ReplacePlantAsync(RoomEntityStore.RoomEntity entity, string className)
    {
        var pet = entity.Pet!;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassName == className) is not { } definition
            || await db.Pets.FirstOrDefaultAsync(p => p.Id == pet.Data.Id) is not { } row)
            return null;

        var item = new UserItem { OwnerId = pet.Data.OwnerId, DefinitionId = definition.Id };
        db.UserItems.Add(item);
        db.Pets.Remove(row);
        await db.SaveChangesAsync();

        foreach (var request in _plantRequests.Keys.Where(k => k.Asking == pet.Data.Id || k.Asked == pet.Data.Id).ToList())
            _plantRequests.Remove(request);
        _entities!.RemovePet(pet.Data.Id);
        Broadcast(new UserRemoveMessageComposer(entity.RoomIndex));
        return item;
    }

    // ---------- breeding plants ----------

    private const int AskToBreed = 0;
    private const int TurnDown = 1;
    private const int AgreeToBreed = 2;

    public async Task BreedPlantsAsync(Guid playerId, int state, int petOneId, int petTwoId)
    {
        if (_entities?.FindPet(petOneId) is not { Pet: { } one } oneEntity || _entities.FindPet(petTwoId) is not { Pet: { } two } twoEntity
            || oneEntity == twoEntity || !IsPlant(one.Data) || !IsPlant(two.Data))
            return;

        switch (state)
        {
            case AskToBreed:
                RefreshPlant(one);
                RefreshPlant(two);
                var (own, other) = one.Data.OwnerId == playerId ? (one, two) : (two, one);
                if (own.Data.OwnerId != playerId || !own.CanBreed || !other.CanBreed)
                    return;

                // one's own plants, or a plant whose owner lets it be bred, need nobody's say
                if (other.Data.OwnerId == playerId || other.Data.BreedingAllowed)
                {
                    await BreedAsync(own, other);
                    return;
                }

                _plantRequests[(own.Data.Id, other.Data.Id)] = playerId;
                SendTo(other.Data.OwnerId, new PetBreedingComposer(PetBreedingComposer.Requested, other.Data.Id, own.Data.Id));
                break;

            case TurnDown:
            case AgreeToBreed:
                // the one answering owns the plant that was asked; anyone else's answer leaves the request standing
                var key = _plantRequests.Keys.FirstOrDefault(k => (k.Asking, k.Asked) == (petOneId, petTwoId) || (k.Asking, k.Asked) == (petTwoId, petOneId));
                if (!_plantRequests.ContainsKey(key))
                    return;

                var (asking, asked) = key.Asking == petOneId ? (one, two) : (two, one);
                if (asked.Data.OwnerId != playerId || !_plantRequests.Remove(key, out var asker))
                    return;

                if (state == TurnDown)
                {
                    SendTo(asker, new PetBreedingComposer(PetBreedingComposer.Cancelled, asking.Data.Id, asked.Data.Id));
                    return;
                }

                SendTo(asker, new PetBreedingComposer(PetBreedingComposer.Accepted, asking.Data.Id, asked.Data.Id));
                RefreshPlant(one);
                RefreshPlant(two);
                if (one.CanBreed && two.CanBreed)
                    await BreedAsync(asking, asked);
                break;
        }
    }

    /// <summary>Breeds two plants: neither breeds again until rebred, one of their owners gets a seed, and
    /// maybe the other one too. Both owners see what came of it.</summary>
    private async Task BreedAsync(RoomPet one, RoomPet two)
    {
        foreach (var parent in new[] { one, two })
            await SavePetAsync(parent, data => data.CanBreed = false);

        var rules = plantOptions.Value;
        var (lucky, other) = _petRandom.Next(2) == 0 ? (one, two) : (two, one);
        var first = await GiveSeedAsync(lucky, one, two);
        var secondSeed = _petRandom.Next(WholeChance) < rules.SecondSeedChance;
        var second = secondSeed ? await GiveSeedAsync(other, one, two) : PetBreedingResultData.None;
        foreach (var breeder in new[] { one.Data.OwnerId, two.Data.OwnerId }.Distinct())
            GrainFactory.GetGrain<IPlayerGrain>(breeder).ProgressAchievementAsync(AchievementNames.MonsterPlantBreeder, 1).Ignore();
        if (!secondSeed)
            GrainFactory.GetGrain<IPlayerGrain>(other.Data.OwnerId).ProgressAchievementAsync(AchievementNames.MonsterPlantUnluckyGardener, 1).Ignore();

        var result = new PetBreedingResultComposer(first, second);
        foreach (var owner in new[] { one.Data.OwnerId, two.Data.OwnerId }.Distinct())
            await GrainFactory.GetGrain<IPlayerGrain>(owner).SendAsync(result.ToMessage());

        foreach (var parent in new[] { one, two })
        {
            if (_entities!.FindPet(parent.Data.Id) is { } entity && RefreshPlant(parent))
                ShowPlant(entity);
        }
    }

    /// <summary>A seed for the plant's owner, holding a look bred from the two parents.</summary>
    private async Task<PetBreedingResultData> GiveSeedAsync(RoomPet receiver, RoomPet one, RoomPet two)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var type = await db.PetTypes.AsNoTracking().Include(t => t.Layers).FirstAsync(t => t.Id == one.Data.TypeId);
        if (await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassName == plantOptions.Value.SeedClassName) is not { } seed
            || await plantLooks.BredAsync(type, one.Data, two.Data, _petRandom) is not { } look)
            return PetBreedingResultData.None;

        var item = new UserItem { OwnerId = receiver.Data.OwnerId, DefinitionId = seed.Id };
        db.UserItems.Add(item);
        await db.SaveChangesAsync();
        db.PlantSeeds.Add(new PlantSeed { ItemId = item.Id, PaletteId = look.PaletteId, CustomParts = look.CustomParts, Rarity = look.Rarity });
        await db.SaveChangesAsync();

        await GrainFactory.GetGrain<IPlayerGrain>(receiver.Data.OwnerId).AnnounceItemsAsync([item.Id]);
        return new PetBreedingResultData(item.Id, seed.ClassId, seed.ClassName, receiver.OwnerAccountId, receiver.OwnerName, look.Rarity, look.Mutated);
    }
}
