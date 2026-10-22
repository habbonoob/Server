using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Pets;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Pets;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Pets;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Looking after pets: eating, drinking and resting at their furniture, dyes, hairstyles and
/// saddles, riding, what their owners allow, and pets that come in a box.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The pet info's access rights: only the owner rides, or anyone does.</summary>
    private const int OwnerRides = 0;
    private const int AnyoneRides = 1;

    /// <summary>A pet product's custom parameters: the kind of pet, then what it changes.</summary>
    private const int ProductPetTypeField = 0;
    private const int SaddleEffectField = 4;
    private const int FieldsPerPart = 3;

    /// <summary>A layer with no part of its own yet keeps none.</summary>
    private const int NoPart = -1;

    /// <summary>The colour a pet out of a box has; its palette colours it.</summary>
    private const string PackagedPetColor = "FFFFFF";

    // ---------- eating, drinking, resting ----------

    /// <summary>Sends the pet to the nearest furniture for it; false when there is none to go to.</summary>
    private async Task<bool> SeekCareAsync(RoomEntityStore.RoomEntity entity, PetCare care)
    {
        if (_items is null || CarePosture(entity.Pet!, care) is null)
            return false;

        var target = _items.Items
            .Where(item => care switch
            {
                PetCare.Eat => item.Interaction is PetFoodInteraction,
                PetCare.Drink => item.Interaction is PetDrinkInteraction && !IsLastState(item),
                _ => item.Interaction is PetNestInteraction,
            })
            .OrderBy(item => Math.Max(Math.Abs(item.X - entity.X), Math.Abs(item.Y - entity.Y)))
            .FirstOrDefault();
        if (target is null)
            return false;

        entity.Pet!.Busy = true;
        entity.Pet.Following = null;
        await WalkThenAsync(entity, target.X, target.Y, () => BeginCareAsync(entity, target, care));

        // a walk that could not start leaves the pet free again
        if (entity.OnArrival is null && !entity.Walking && entity.Pet.Posture is null)
            entity.Pet.Busy = false;
        return true;
    }

    /// <summary>Hungry or tired, the pet goes to eat or rest by itself; false when it needs neither, or
    /// there is nothing for it.</summary>
    private async Task<bool> SeeToNeedsAsync(RoomEntityStore.RoomEntity entity)
    {
        var rules = petOptions.Value;
        var data = entity.Pet!.Data;
        var now = DateTimeOffset.UtcNow;
        if (petRules.Nutrition(data, now) * 100 < petRules.MaxNutrition(data.Level) * rules.HungryBelowPercent
            && await SeekCareAsync(entity, PetCare.Eat))
            return true;
        return petRules.Energy(data, now) * 100 < petRules.MaxEnergy(data.Level) * rules.TiredBelowPercent
               && await SeekCareAsync(entity, PetCare.Rest);
    }

    /// <summary>The first posture of the kind's for the care, by the configured order.</summary>
    private string? CarePosture(RoomPet pet, PetCare care) =>
        (care == PetCare.Rest ? petOptions.Value.RestPostures : petOptions.Value.FeedPostures)
        .FirstOrDefault(pet.Type.Postures.Contains);

    private Task BeginCareAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem item, PetCare care)
    {
        var pet = entity.Pet!;
        if (_entities?.FindPet(pet.Data.Id) != entity || !_furniture!.IsPlaced(item) || !RoomFurnitureUse.Reaches(entity, item)
            || CarePosture(pet, care) is not { } posture)
        {
            pet.Busy = false;
            return Task.CompletedTask;
        }

        var rules = petOptions.Value;
        LookAt(entity, item.X, item.Y);
        pet.Posture = posture;
        pet.PostureUntilTick = 0;
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));

        var seconds = care switch { PetCare.Eat => rules.EatSeconds, PetCare.Drink => rules.DrinkSeconds, _ => rules.RestSeconds };
        Schedule(TimeSpan.FromSeconds(seconds), () => FinishCareAsync(entity, item, care, posture));
        return Task.CompletedTask;
    }

    /// <summary>Done eating, drinking or resting: the pet has what it came for, and the furniture shows
    /// what it gave. Food eaten to the last state is gone.</summary>
    private async Task FinishCareAsync(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem item, PetCare care, string posture)
    {
        var pet = entity.Pet!;
        pet.Busy = false;
        if (_entities?.FindPet(pet.Data.Id) != entity || pet.Posture != posture)
            return;

        _entities.ClearPosture(entity);
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));

        var rules = petOptions.Value;
        var now = DateTimeOffset.UtcNow;
        var fed = _furniture!.IsPlaced(item) || care == PetCare.Rest;
        if (!fed)
            return;

        await SavePetAsync(pet, row =>
        {
            petRules.Settle(row, now);
            switch (care)
            {
                case PetCare.Eat:
                    row.Nutrition = Math.Min(row.Nutrition + rules.FoodNutrition, petRules.MaxNutrition(row.Level));
                    break;
                case PetCare.Drink:
                    row.Energy = Math.Min(row.Energy + rules.DrinkEnergy, petRules.MaxEnergy(row.Level));
                    break;
                default:
                    row.Energy = petRules.MaxEnergy(row.Level);
                    break;
            }
        });

        if (care == PetCare.Eat)
        {
            foreach (var achievement in PetAchievements.Feeding(pet.Data.TypeId))
                GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(achievement, rules.FoodNutrition).Ignore();
        }

        if (care == PetCare.Rest || !_furniture.IsPlaced(item))
            return;

        if (care == PetCare.Eat && IsLastState(item))
            await _furniture.RemoveAsync(item);
        else if (NextState(item) is { } next)
            await _furniture.UpdateAsync(item, item.Data with { State = next.ToString(CultureInfo.InvariantCulture) });
    }

    private static List<int> States(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.States.Select(s => s.State).Where(state => state >= 0).Order().ToList();

    private static int CurrentState(RoomItemStore.PlacedItem item) =>
        int.TryParse(item.Data.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ? state : 0;

    private static bool IsLastState(RoomItemStore.PlacedItem item) =>
        States(item) is { Count: > 0 } states && CurrentState(item) >= states[^1];

    private static int? NextState(RoomItemStore.PlacedItem item) =>
        States(item).Where(state => state > CurrentState(item)).Select(state => (int?)state).FirstOrDefault();

    /// <summary>The player hands what they carry to their pet, which eats it.</summary>
    public async Task PassHandItemToPetAsync(Guid playerId, int petId)
    {
        if (_entities?.Find(playerId) is not { HandItem: not 0 } giver || _entities.FindPet(petId) is not { Pet: { } pet } entity
            || pet.Data.OwnerId != playerId || pet.Busy || entity.Rider is not null)
            return;

        var (x, y) = NearestFreeNeighbour(entity, giver);
        await WalkThenAsync(giver, x, y, async () =>
        {
            if (giver.HandItem == 0 || _entities.FindPet(petId) != entity)
                return;

            LookAt(giver, entity.X, entity.Y);
            LookAt(entity, giver.X, giver.Y);
            SetHandItem(giver, 0);
            var now = DateTimeOffset.UtcNow;
            await SavePetAsync(pet, row =>
            {
                petRules.Settle(row, now);
                row.Nutrition = Math.Min(row.Nutrition + petOptions.Value.HandItemNutrition, petRules.MaxNutrition(row.Level));
            });
            // the giver fed a pet; the owner's own kind of pet ate
            foreach (var achievement in PetAchievements.Feeding(pet.Data.TypeId))
            {
                var feeder = achievement == AchievementNames.PetFeeding ? giver.Player.Id : pet.Data.OwnerId;
                GrainFactory.GetGrain<IPlayerGrain>(feeder).ProgressAchievementAsync(achievement, petOptions.Value.HandItemNutrition).Ignore();
            }

            if (CarePosture(pet, PetCare.Eat) is { } posture)
            {
                pet.Posture = posture;
                pet.PostureUntilTick = _entities.CurrentTick + petOptions.Value.EatSeconds * TicksPerSecond;
                Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
            }
        });
    }

    // ---------- dyes, hairstyles, saddles ----------

    /// <summary>Uses a pet product — one in the room or in the player's inventory — on the player's pet.
    /// The product is used up.</summary>
    public async Task CustomizePetAsync(Guid playerId, int itemId, int petId)
    {
        if (_room is null || _items is null || _entities?.FindPet(petId) is not { Pet: { } pet } entity || pet.Data.OwnerId != playerId)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var placed = _items.Find(itemId);
        var row = await db.UserItems.Include(i => i.Definition)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == playerId && !i.InMarketplace && i.ChestId == null && (i.RoomId == null || i.RoomId == _room.Id));
        if (row?.Definition is not { } product || (row.RoomId is not null && placed is null)
            || !Enum.IsDefined((PetProductKind)product.SpecialType))
            return;

        var fields = product.CustomParams.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length <= ProductPetTypeField || !int.TryParse(fields[ProductPetTypeField], out var typeId) || typeId != pet.Data.TypeId)
            return;

        var kind = (PetProductKind)product.SpecialType;
        if (kind == PetProductKind.Saddle && pet.Data.SaddleDefinitionId is not null)
            return;

        // a plant's potions change how it is, not how it looks
        if (kind is PetProductKind.Revival or PetProductKind.Rebreed or PetProductKind.Fertiliser)
        {
            if (!await TreatPlantAsync(entity, kind, product.ClassName))
                return;
        }
        else
        {
            var (palette, parts) = Customized(pet, kind, fields);
            if (palette is null && parts is null)
                return;

            await SavePetAsync(pet, data =>
            {
                data.PaletteId = palette ?? data.PaletteId;
                data.CustomParts = parts ?? data.CustomParts;
                if (kind == PetProductKind.Saddle)
                    data.SaddleDefinitionId = product.Id;
            });
        }


        if (placed is not null)
            await _furniture!.RemoveAsync(placed);
        else
        {
            db.UserItems.Remove(row);
            await db.SaveChangesAsync();
            SendTo(playerId, FurniListRemoveComposer.Item(itemId));
        }

        ShowPetFigure(entity);
    }

    /// <summary>What the product makes of the pet's palette and parts; null for what it leaves alone, and
    /// both null when it does nothing to this pet.</summary>
    private (int? Palette, List<int>? Parts) Customized(RoomPet pet, PetProductKind kind, string[] fields)
    {
        var data = pet.Data;
        switch (kind)
        {
            case PetProductKind.Dye when fields.Length > 1:
                var tag = petOptions.Value.ColorTags.IndexOf(fields[1]);
                var breed = pet.Type.Palettes.FirstOrDefault(p => p.PaletteId == data.PaletteId)?.Breed;
                var dyed = pet.Type.Palettes.FirstOrDefault(p => tag >= 0 && p.ColorTag == tag && p.Breed == breed);
                return (dyed?.PaletteId, null);

            case PetProductKind.Hairstyle or PetProductKind.Saddle when fields.Length > 3:
                return (null, WithParts(data.CustomParts, Numbers(fields[1]), Numbers(fields[2]), Numbers(fields[3]), keepPalette: kind == PetProductKind.Hairstyle));

            case PetProductKind.HairDye when fields.Length > 2:
                var layers = Numbers(fields[1]);
                var palettes = Numbers(fields[2]);
                var keptParts = layers.Select(layer => PartOn(data.CustomParts, layer)?.Part ?? NoPart).ToList();
                return (null, WithParts(data.CustomParts, layers, keptParts, palettes, keepPalette: false));

            default:
                return (null, null);
        }
    }

    private static List<int> Numbers(string field) =>
        field.Split(',').Select(value => int.TryParse(value, out var number) ? number : NoPart).ToList();

    private static (int Part, int Palette)? PartOn(List<int> customParts, int layer)
    {
        for (var i = 0; i + FieldsPerPart - 1 < customParts.Count; i += FieldsPerPart)
        {
            if (customParts[i] == layer)
                return (customParts[i + 1], customParts[i + 2]);
        }

        return null;
    }

    /// <summary>The parts with each layer given replaced; a hairstyle keeps the palette a layer already had.</summary>
    private static List<int>? WithParts(List<int> customParts, List<int> layers, List<int> parts, List<int> palettes, bool keepPalette)
    {
        if (layers.Count == 0 || parts.Count < layers.Count || palettes.Count < layers.Count)
            return null;

        var result = new List<int>();
        for (var i = 0; i + FieldsPerPart - 1 < customParts.Count; i += FieldsPerPart)
        {
            if (!layers.Contains(customParts[i]))
                result.AddRange(customParts.Skip(i).Take(FieldsPerPart));
        }

        for (var i = 0; i < layers.Count; i++)
        {
            var palette = keepPalette && PartOn(customParts, layers[i]) is { } existing ? existing.Palette : palettes[i];
            result.AddRange([layers[i], parts[i], palette]);
        }

        return result;
    }

    /// <summary>Shows the pet's look, saddle and rider to everyone.</summary>
    private void ShowPetFigure(RoomEntityStore.RoomEntity entity)
    {
        var pet = entity.Pet!;
        entity.Player = pet.Avatar(PetFigureWireData.FigureString(pet.Data));
        Broadcast(new PetFigureUpdateComposer(entity.RoomIndex, pet.Data.Id, PetFigureWireData.From(pet.Data),
            pet.Data.SaddleDefinitionId is not null, entity.Rider is not null));
    }

    /// <summary>Takes the saddle off the owner's pet: its parts come off, and it goes back to the owner.</summary>
    public async Task RemoveSaddleAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { Data.SaddleDefinitionId: { } saddleId } pet } entity || pet.Data.OwnerId != playerId)
            return;

        if (entity.Rider is { } rider)
            Dismount(rider);

        await using var db = await dbFactory.CreateDbContextAsync();
        var saddle = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == saddleId);
        var fields = saddle?.CustomParams.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var layers = fields.Length > 1 ? Numbers(fields[1]) : [];

        await SavePetAsync(pet, data =>
        {
            data.SaddleDefinitionId = null;
            data.CustomParts = Enumerable.Range(0, data.CustomParts.Count / FieldsPerPart)
                .Where(i => !layers.Contains(data.CustomParts[i * FieldsPerPart]))
                .SelectMany(i => data.CustomParts.Skip(i * FieldsPerPart).Take(FieldsPerPart))
                .ToList();
        });

        var item = new UserItem { OwnerId = playerId, DefinitionId = saddleId };
        db.UserItems.Add(item);
        await db.SaveChangesAsync();
        await GrainFactory.GetGrain<IPlayerGrain>(playerId).AnnounceItemsAsync([item.Id]);
        ShowPetFigure(entity);
    }

    // ---------- riding ----------

    public async Task MountPetAsync(Guid playerId, int petId, bool mount)
    {
        if (_entities?.Find(playerId) is not { } user)
            return;

        if (!mount)
        {
            if (user.Mount?.Pet?.Data.Id == petId)
                Dismount(user);
            return;
        }

        if (user.Mount is not null || _entities.FindPet(petId) is not { Pet: { Data.SaddleDefinitionId: { } saddleId } pet } entity
            || entity.Rider is not null || (pet.Data.OwnerId != playerId && !pet.Data.AnyoneCanRide))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var saddle = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == saddleId);
        var fields = saddle?.CustomParams.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var effect = fields.Length > SaddleEffectField && int.TryParse(fields[SaddleEffectField], out var id) ? id : 0;

        var (x, y) = NearestFreeNeighbour(entity, user);
        await WalkThenAsync(user, x, y, () =>
        {
            if (_entities.FindPet(petId) != entity || entity.Rider is not null || user.Mount is not null
                || Math.Abs(user.X - entity.X) > 1 || Math.Abs(user.Y - entity.Y) > 1)
                return Task.CompletedTask;

            _entities.ClearPosture(entity);
            if (entity.Walking)
                _entities.Warp(entity, entity.X, entity.Y);
            pet.Busy = false;
            pet.Following = null;
            user.Mount = entity;
            entity.Rider = user;
            if (user.Player.Id != pet.Data.OwnerId)
                GrainFactory.GetGrain<IPlayerGrain>(pet.Data.OwnerId).ProgressAchievementAsync(AchievementNames.HorseRent, 1).Ignore();
            _entities.Carry(user, entity.X, entity.Y);
            user.BodyDirection = user.HeadDirection = entity.BodyDirection;
            ApplyEffect(user, effect);
            Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(user), _entities.CurrentStatus(entity)]));
            ShowPetFigure(entity);
            return Task.CompletedTask;
        });
    }

    /// <summary>Gets the rider down beside the pet, out of the riding effect.</summary>
    private void Dismount(RoomEntityStore.RoomEntity rider)
    {
        if (rider.Mount is not { } entity || _entities is null)
            return;

        rider.Mount = null;
        entity.Rider = null;
        if (entity.Walking)
            _entities.Warp(entity, entity.X, entity.Y);

        var beside = Enumerable.Range(0, Directions)
            .Select(RoomEntityStore.Offset)
            .Select(offset => (X: entity.X + offset.X, Y: entity.Y + offset.Y))
            .FirstOrDefault(tile => _model!.IsTile(tile.X, tile.Y) && !_items!.BlocksWalking(tile.X, tile.Y) && !_entities.HasAvatarAt(tile.X, tile.Y),
                (entity.X, entity.Y));
        Broadcast(UserUpdateMessageComposer.From([_entities.Warp(rider, beside.X, beside.Y), _entities.CurrentStatus(entity)]));
        ApplyEffect(rider, 0);
        ShowPetFigure(entity);
    }

    // ---------- what the owner allows ----------

    public async Task TogglePetRidingAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { } pet } || pet.Data.OwnerId != playerId)
            return;

        await SavePetAsync(pet, data => data.AnyoneCanRide = !data.AnyoneCanRide);
        await SendPetInfoAsync(playerId, petId);
    }

    public async Task TogglePetBreedingAsync(Guid playerId, int petId)
    {
        if (_entities?.FindPet(petId) is not { Pet: { } pet } entity || pet.Data.OwnerId != playerId)
            return;

        await SavePetAsync(pet, data => data.BreedingAllowed = !data.BreedingAllowed);
        Broadcast(new PetStatusUpdateComposer(entity.RoomIndex, petId, CanBreed: false, CanHarvest: false, CanRevive: false, pet.Data.BreedingAllowed));
    }

    // ---------- pets in boxes ----------

    /// <summary>The owner opened a box with a pet in it: they are shown the pet, to name it.</summary>
    private async Task<bool> OfferPackagedPetAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not PetPackageInteraction)
            return false;

        if (item.Item.OwnerId == user.Player.Id && await PackagedPetAsync(item) is { } pet)
            SendTo(user.Player.Id, new OpenPetPackageRequestedMessageComposer(item.Item.Id, PetFigureWireData.From(pet)));
        return true;
    }

    /// <summary>The pet the box holds: of its kind, in one of its palettes, the same one each time it is asked.</summary>
    private async Task<Pet?> PackagedPetAsync(RoomItemStore.PlacedItem item)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.PetPackages.AsNoTracking().FirstOrDefaultAsync(p => p.DefinitionId == item.Item.DefinitionId) is not { } package)
            return null;

        var palettes = package.Palettes.Count > 0
            ? package.Palettes
            : await db.PetPalettes.Where(p => p.TypeId == package.TypeId && p.Sellable).OrderBy(p => p.PaletteId).Select(p => p.PaletteId).ToListAsync();
        if (palettes.Count == 0)
            return null;

        return new Pet
        {
            TypeId = package.TypeId, PaletteId = palettes[item.Item.Id % palettes.Count], Color = PackagedPetColor,
            Type = await db.PetTypes.AsNoTracking().Include(t => t.Palettes).FirstAsync(t => t.Id == package.TypeId),
        };
    }

    /// <summary>Names the pet in the box: a name that will not do is turned away; otherwise the box is gone
    /// and the pet stands where it stood.</summary>
    public async Task OpenPetPackageAsync(Guid playerId, int itemId, string name)
    {
        if (_room is null || _items?.Find(itemId) is not { Interaction: PetPackageInteraction } item || item.Item.OwnerId != playerId || item.Temporary
            || _entities?.Find(playerId) is not { } owner || await PackagedPetAsync(item) is not { } pet)
            return;

        var (result, info) = await petNames.CheckAsync(name);
        if (result != ApproveNameMessageComposer.Accepted)
        {
            SendTo(playerId, new OpenPetPackageResultMessageComposer(itemId, result, info));
            return;
        }

        var (x, y, rotation) = (item.X, item.Y, item.Rotation);
        if (!await _furniture!.RemoveAsync(item))
            return;

        var type = pet.Type!;
        pet.Type = null;
        pet.OwnerId = playerId;
        pet.Name = name;
        pet.RoomId = _room.Id;
        pet.X = x;
        pet.Y = y;
        pet.Rotation = rotation;
        pet.CreatedAt = DateTimeOffset.UtcNow;
        petRules.Start(pet, pet.CreatedAt);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        pet.Type = type;

        var entity = AddPet(pet, owner.Player.AccountId, owner.Player.Username, x, y, rotation);
        Broadcast(UsersMessageComposer.From([_entities.ToWireData(entity)]));
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
        SendTo(playerId, new OpenPetPackageResultMessageComposer(itemId, ApproveNameMessageComposer.Accepted, string.Empty));
    }
}
