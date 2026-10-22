using System.Globalization;
using Habbo.Domain.Pets;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Breeding in nests: a pet told to breed goes to a nest for its kind; with two in it, the nest's
/// owner is asked, and a baby of the nest's baby kind comes out, rarer the more experienced the parents.
/// The nest is used up.</summary>
public sealed partial class RoomGrain
{
    /// <summary>A nest holds two parents.</summary>
    private const int NestCapacity = 2;

    /// <summary>A nest's custom parameters: the adult kind first, the baby kind last.</summary>
    private const char NestFieldSeparator = ',';

    private const string BabyColor = "FFFFFF";

    private const int WholeChance = 100;

    /// <summary>The pets in each nest, by the nest's item id.</summary>
    private readonly Dictionary<int, List<RoomEntityStore.RoomEntity>> _nests = [];

    private static (int Adult, int Baby)? NestKinds(RoomItemStore.PlacedItem nest)
    {
        var fields = nest.Item.Definition!.CustomParams.Split(NestFieldSeparator);
        return fields.Length >= 2 && int.TryParse(fields[0], out var adult) && int.TryParse(fields[^1], out var baby)
            ? (adult, baby)
            : null;
    }

    private List<RoomEntityStore.RoomEntity> Occupants(int nestId) =>
        _nests.TryGetValue(nestId, out var pets) ? pets : [];

    /// <summary>Sends the pet to a nest for its kind: one with a pet of its owner's waiting in it first,
    /// then an empty one. Says why not when it cannot go.</summary>
    private async Task<bool> GoToBreedingNestAsync(RoomEntityStore.RoomEntity entity, RoomEntityStore.RoomEntity speaker)
    {
        var pet = entity.Pet!;
        var nests = _items!.Items.Where(item => item.Interaction is PetBreedingNestInteraction).ToList();
        var suitable = nests.Where(nest => NestKinds(nest)?.Adult == pet.Data.TypeId).ToList();
        var waiting = suitable.FirstOrDefault(nest => Occupants(nest.Item.Id).Count is > 0 and < NestCapacity);
        var empty = suitable.FirstOrDefault(nest => Occupants(nest.Item.Id).Count == 0);

        int? failure = nests.Count == 0 ? GoToBreedingNestFailureComposer.NoNests
            : suitable.Count == 0 ? GoToBreedingNestFailureComposer.NoSuitableNest
            : _nests.Values.Any(pets => pets.Contains(entity)) ? GoToBreedingNestFailureComposer.AlreadyInNest
            : petRules.Energy(pet.Data, DateTimeOffset.UtcNow) < petOptions.Value.BreedingEnergy ? GoToBreedingNestFailureComposer.TooTired
            : waiting is null && empty is null ? GoToBreedingNestFailureComposer.NestFull
            : waiting is not null && empty is null && waiting.Item.OwnerId != pet.Data.OwnerId ? GoToBreedingNestFailureComposer.NotNestOwner
            : null;
        if (failure is { } reason)
        {
            SendTo(speaker.Player.Id, new GoToBreedingNestFailureComposer(reason));
            return false;
        }

        // the second pet in a nest is its owner's own
        var nest = waiting is not null && waiting.Item.OwnerId == pet.Data.OwnerId ? waiting : empty ?? waiting!;
        pet.Busy = true;
        pet.Following = null;
        _nests[nest.Item.Id] = [.. Occupants(nest.Item.Id), entity];
        await WalkThenAsync(entity, nest.X, nest.Y, () => SettleInNestAsync(entity, nest, speaker.Player.Id));
        return true;
    }

    private async Task SettleInNestAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem nest, Guid commanderId)
    {
        var pet = entity.Pet!;
        if (_entities?.FindPet(pet.Data.Id) != entity || !Occupants(nest.Item.Id).Contains(entity))
            return;
        // the nest went while the pet walked to it: the pet is free again rather than kept waiting for it
        if (!_furniture!.IsPlaced(nest))
        {
            LeaveBreedingNest(entity);
            return;
        }

        if (!RoomFurnitureUse.Reaches(entity, nest))
        {
            LeaveBreedingNest(entity);
            SendTo(commanderId, new GoToBreedingNestFailureComposer(GoToBreedingNestFailureComposer.NoWayToNest));
            return;
        }

        pet.Staying = true;
        if (CarePosture(pet, Pets.PetCare.Rest) is { } posture)
        {
            pet.Posture = posture;
            pet.PostureUntilTick = 0;
            Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
        }

        // a pet still on its way is not in the nest yet: the last to settle has the owner asked
        var occupants = Occupants(nest.Item.Id);
        var settled = occupants.Count(o => o.Pet!.Staying);
        await ShowNestFillingAsync(nest, settled);
        if (settled == NestCapacity)
            await AskToBreedAsync(nest, occupants[0], occupants[1]);
    }

    /// <summary>The nest shows how many pets are in it, as far as its states go.</summary>
    private async Task ShowNestFillingAsync(RoomItemStore.PlacedItem nest, int occupants)
    {
        var states = States(nest);
        if (states.Count == 0 || !_furniture!.IsPlaced(nest))
            return;

        var state = states[Math.Min(occupants, states.Count - 1)];
        await _furniture.UpdateAsync(nest, nest.Data with { State = state.ToString(CultureInfo.InvariantCulture) });
    }

    /// <summary>Asks the nest's owner whether to breed the two pets in it, showing how rare the baby may be.</summary>
    private async Task AskToBreedAsync(RoomItemStore.PlacedItem nest, RoomEntityStore.RoomEntity one, RoomEntityStore.RoomEntity two)
    {
        if (NestKinds(nest) is not { } kinds)
            return;

        var categories = await RarityChancesAsync(kinds.Baby, one.Pet!.Data.Level, two.Pet!.Data.Level);
        SendTo(nest.Item.OwnerId, new ConfirmBreedingRequestComposer(nest.Item.Id, BreedingInfo(one), BreedingInfo(two),
            categories.Select(c => new RarityCategoryData((int)Math.Round(c.Chance), c.Category.Palettes)).ToList(), kinds.Baby));
    }

    private static BreedingPetInfoData BreedingInfo(RoomEntityStore.RoomEntity entity) =>
        new(entity.Pet!.Data.Id, entity.Pet.Data.Name, entity.Pet.Data.Level, PetFigureWireData.FigureString(entity.Pet.Data), entity.Pet.OwnerName);

    /// <summary>Each rarity category of the baby kind with its chance for parents of these levels, rarest
    /// first; the commonest takes whatever chance is left.</summary>
    private async Task<List<(PetRarityCategory Category, double Chance)>> RarityChancesAsync(int babyType, int levelOne, int levelTwo)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var categories = await db.PetRarityCategories.AsNoTracking().Where(c => c.BabyTypeId == babyType).OrderBy(c => c.Rank).ToListAsync();
        var averageLevel = (levelOne + levelTwo) / 2.0;
        var result = new List<(PetRarityCategory, double)>();
        var left = (double)WholeChance;
        for (var i = 0; i < categories.Count; i++)
        {
            var chance = i == categories.Count - 1
                ? left
                : Math.Clamp(categories[i].Chance + categories[i].ChancePerLevel * averageLevel, 0, left);
            left -= chance;
            result.Add((categories[i], chance));
        }

        return result;
    }

    public async Task ConfirmPetBreedingAsync(Guid playerId, int nestId, string name, int petOneId, int petTwoId)
    {
        if (_room is null || _entities?.Find(playerId) is not { } owner)
            return;

        if (_items?.Find(nestId) is not { Interaction: PetBreedingNestInteraction } nest || nest.Item.OwnerId != playerId || nest.Temporary
            || NestKinds(nest) is not { } kinds)
        {
            SendTo(playerId, new ConfirmBreedingResultComposer(nestId, ConfirmBreedingResultComposer.NoNest));
            return;
        }

        var occupants = Occupants(nestId);
        var ids = occupants.Select(o => o.Pet!.Data.Id).ToHashSet();
        if (occupants.Count != NestCapacity || !ids.SetEquals([petOneId, petTwoId]) || occupants.Any(o => _entities.FindPet(o.Pet!.Data.Id) != o))
        {
            SendTo(playerId, new ConfirmBreedingResultComposer(nestId, ConfirmBreedingResultComposer.PetsMissing));
            return;
        }

        if ((await petNames.CheckAsync(name)).Result != ApproveNameMessageComposer.Accepted)
        {
            SendTo(playerId, new ConfirmBreedingResultComposer(nestId, ConfirmBreedingResultComposer.NameInvalid));
            return;
        }

        // the rarer categories come up by their chances, the commonest otherwise
        var categories = await RarityChancesAsync(kinds.Baby, occupants[0].Pet!.Data.Level, occupants[1].Pet!.Data.Level);
        var roll = _petRandom.NextDouble() * WholeChance;
        var drawn = categories.FirstOrDefault(c => (roll -= c.Chance) < 0 && c.Category.Palettes.Count > 0);
        if (drawn.Category is null)
            drawn = categories.LastOrDefault(c => c.Category.Palettes.Count > 0);
        if (drawn.Category is null)
            return;

        var (x, y, rotation) = (nest.X, nest.Y, nest.Rotation);
        foreach (var parent in occupants.ToList())
        {
            var now = DateTimeOffset.UtcNow;
            await SavePetAsync(parent.Pet!, row =>
            {
                petRules.Settle(row, now);
                row.Energy = Math.Max(0, row.Energy - petOptions.Value.BreedingEnergy);
            });
            LeaveBreedingNest(parent);
        }

        _nests.Remove(nestId);
        if (!await _furniture!.RemoveAsync(nest))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var baby = new Pet
        {
            OwnerId = playerId, TypeId = kinds.Baby, Name = name, Color = BabyColor, RoomId = _room.Id, X = x, Y = y, Rotation = rotation,
            PaletteId = drawn.Category.Palettes[_petRandom.Next(drawn.Category.Palettes.Count)], CreatedAt = DateTimeOffset.UtcNow,
        };
        petRules.Start(baby, baby.CreatedAt);
        db.Pets.Add(baby);
        await db.SaveChangesAsync();
        baby.Type = await db.PetTypes.AsNoTracking().Include(t => t.Palettes).FirstAsync(t => t.Id == kinds.Baby);

        var entity = AddPet(baby, owner.Player.AccountId, owner.Player.Username, x, y, rotation);
        Broadcast(UsersMessageComposer.From([_entities.ToWireData(entity)]));
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
        SendTo(playerId, new ConfirmBreedingResultComposer(nestId, ConfirmBreedingResultComposer.Bred));
        if (PetAchievements.Breeders.FirstOrDefault(breeder => breeder.BabyType == kinds.Baby).Achievement is { } bred)
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(bred, 1).Ignore();
        SendTo(playerId, new NestBreedingSuccessComposer(baby.Id, drawn.Category.Rank));
    }

    public async Task CancelPetBreedingAsync(Guid playerId, int nestId)
    {
        if (_items?.Find(nestId) is not { } nest || nest.Item.OwnerId != playerId)
            return;

        foreach (var pet in Occupants(nestId).ToList())
            LeaveBreedingNest(pet);
        await ShowNestFillingAsync(nest, 0);
    }

    /// <summary>A nest leaving the room lets out the pets in it and on their way to it.</summary>
    private void EmptyBreedingNest(int nestId)
    {
        foreach (var pet in Occupants(nestId).ToList())
            LeaveBreedingNest(pet);
    }

    /// <summary>Lets the pet out of whatever nest it is in, or on its way to, free to go about again.</summary>
    private void LeaveBreedingNest(RoomEntityStore.RoomEntity entity)
    {
        foreach (var (nestId, pets) in _nests.ToList())
        {
            if (!pets.Remove(entity))
                continue;
            if (pets.Count == 0)
                _nests.Remove(nestId);

            var pet = entity.Pet!;
            pet.Busy = false;
            pet.Staying = false;
            entity.OnArrival = null;
            if (_entities?.ClearPosture(entity) == true)
                Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
        }
    }
}
