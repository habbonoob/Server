using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Rooms;
using Habbo.Domain.Rooms.Copies;
using Habbo.Infrastructure;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Grains.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items;
using Habbo.Silo.Wired;
using Habbo.Silo.Wired.Variables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Rooms.Copies;

/// <summary>Builds a copied room (<see cref="RoomCopy"/>) here, owned by the one importing it: its floor plan as a model
/// of its own, as the floor plan editor saves one, its walls and floor, and a new item for every furni this hotel has,
/// placed where it stood and showing what it showed. The rest are counted and left out. Wired boxes this hotel has are
/// set up as their dialogs showed them, picking the new items and using the room's new variables. Nothing is changed
/// when the floor plan wouldn't pass the editor.</summary>
public sealed class RoomImporter(
    IDbContextFactory<AppDbContext> dbFactory, IOptions<RoomOptions> options, WiredBoxRegistry wiredBoxes)
{
    /// <summary>What the left-out furni standing outside the floor plan are counted under.</summary>
    public const string OffThePlan = "(off the floor plan)";

    /// <summary>The most furni one import places, past any room Habbo builds.</summary>
    public const int MaxItems = 10_000;

    public async Task<RoomImportResult> ImportAsync(Guid ownerId, RoomCopy copy, CancellationToken ct = default)
    {
        var rules = options.Value;
        var name = Clip(copy.Name.Trim(), rules.NameMaxLength);
        if (name.Length == 0)
            name = $"Room {copy.SourceRoomId}";

        var plan = new FloorPlan(copy.Heightmap, copy.DoorX, copy.DoorY, copy.DoorDirection,
            copy.WallThickness, copy.FloorThickness, copy.FixedWallsHeight);
        // a plan Habbo built passes as large as the editor goes; nothing stands on it yet
        var errors = FloorPlanValidator.Validate(plan, rules, largePlansAllowed: true, kept: [],
            RoomModelParser.Parse(copy.Heightmap, copy.DoorX, copy.DoorY), out var heightmap);
        if (copy.FloorItems.Count + copy.WallItems.Count > MaxItems)
            errors.Add($"more than {MaxItems} furni");
        if (errors.Count > 0)
            return new RoomImportResult(null, name, 0, 0, new Dictionary<string, int>(), errors);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // the copy's category is the other hotel's: the room gets the first one any player may pick, as a new room's
        // window offers it, or the owner is asked for one on entering
        var category = await db.NavigatorCategories.AsNoTracking()
            .Where(c => c.Visible && !c.Automatic && !c.StaffOnly && !c.IsEventCategory)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);

        var model = new RoomModelData
        {
            Name = RoomGrain.CustomModelPrefix + Guid.NewGuid().ToString("N"),
            Heightmap = heightmap,
            DoorX = copy.DoorX,
            DoorY = copy.DoorY,
            DoorDirection = copy.DoorDirection,
        };
        db.RoomModels.Add(model);
        var room = new Room
        {
            OwnerId = ownerId,
            Model = model,
            Name = name,
            Description = Clip(copy.Description.Trim(), rules.DescriptionMaxLength),
            Wallpaper = copy.Wallpaper,
            FloorType = copy.Floor,
            Landscape = copy.Landscape.Length > 0 ? copy.Landscape : Room.NoLandscape,
            WallsHidden = copy.WallsHidden,
            WallThicknessExponent = copy.WallThickness,
            FloorThicknessExponent = copy.FloorThickness,
            FixedWallsHeight = copy.FixedWallsHeight,
            UseSmallScale = copy.UseSmallScale,
            CategoryId = category ?? -1,
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        // named after its room, as the editor would have
        model.Name = RoomGrain.CustomModelPrefix + room.Id;

        var definitions = await db.ItemDefinitions.AsNoTracking()
            .Select(d => new { d.Id, d.ClassId, d.ClassName, d.Type })
            .ToListAsync(ct);
        var classNames = definitions.ToDictionary(d => d.Id, d => d.ClassName);
        var byName = definitions.GroupBy(d => d.ClassName).ToDictionary(g => g.Key, g => g.First().Id);
        var byClass = definitions.GroupBy(d => (d.ClassId, d.Type)).ToDictionary(g => g.Key, g => g.First().Id);
        var missing = new Dictionary<string, int>();

        int? Find(string? className, int classId, ItemType type, string kind)
        {
            if (className is not null && byName.TryGetValue(className, out var named))
                return named;
            if (byClass.TryGetValue((classId, type), out var numbered))
                return numbered;
            var key = className ?? $"{kind}:{classId}";
            missing[key] = missing.GetValueOrDefault(key) + 1;
            return null;
        }

        var parsed = RoomModelParser.Parse(heightmap, copy.DoorX, copy.DoorY);
        var placedFloor = new List<(RoomCopyFloorItem Copy, UserItem Item)>();
        var placedWall = new List<(RoomCopyWallItem Copy, UserItem Item)>();
        var floorItems = 0;
        foreach (var furni in copy.FloorItems)
        {
            if (Find(furni.ClassName, furni.ClassId, ItemType.Floor, "floor") is not { } definitionId)
                continue;
            if (furni.X < 0 || furni.Y < 0 || furni.X >= parsed.SizeX || furni.Y >= parsed.SizeY)
            {
                missing[OffThePlan] = missing.GetValueOrDefault(OffThePlan) + 1;
                continue;
            }
            var item = new UserItem
            {
                OwnerId = ownerId,
                DefinitionId = definitionId,
                RoomId = room.Id,
                X = furni.X,
                Y = furni.Y,
                Z = furni.Z,
                Rotation = furni.Direction,
            };
            db.UserItems.Add(item);
            FurniDataStore.Attach(db, item, View(furni.Data));
            placedFloor.Add((furni, item));
            floorItems++;
        }

        var wallItems = 0;
        foreach (var furni in copy.WallItems)
        {
            if (Find(furni.ClassName, furni.ClassId, ItemType.Wall, "wall") is not { } definitionId)
                continue;
            var item = new UserItem
            {
                OwnerId = ownerId,
                DefinitionId = definitionId,
                RoomId = room.Id,
                WallPosition = furni.Location,
            };
            db.UserItems.Add(item);
            FurniDataStore.Attach(db, item, FurniDataView.WithState(furni.State));
            placedWall.Add((furni, item));
            wallItems++;
        }

        // the items have their ids now, which the wired boxes pick them by
        await db.SaveChangesAsync(ct);
        var boxes = placedFloor
            .Where(p => p.Copy.Wired is not null)
            .Select(p => (p.Copy, p.Item, Type: wiredBoxes.For(classNames[p.Item.DefinitionId])))
            .Where(p => p.Type is not null)
            .Select(p => (p.Copy, p.Item, Type: p.Type!))
            .ToList();
        // the boxes that hand out what is worth something need the importer to hold what their saves need
        var held = new HashSet<string>();
        foreach (var permission in boxes.Select(b => b.Type.Permission).OfType<string>().Distinct())
        {
            if (await PermissionQuery.HasAsync(db, ownerId, permission))
                held.Add(permission);
        }
        var (wiredBoxCount, refused) = SetUpWired(db, boxes, placedFloor, placedWall, copy.Variables ?? [], held);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new RoomImportResult(room.Id, room.Name, floorItems, wallItems, missing, [], wiredBoxCount, refused);
    }

    /// <summary>Each copied box this hotel implements, set up as it was and checked as a save would check it: its picks
    /// and the room's variables by their new ids (those not copied are left out), its numbers and text within what a
    /// save allows, its sources and delay as its dialog offers them, and, for the boxes that remember the furni they
    /// pick, the furni as they now stand. A box whose settings a save would refuse, or whose save needs a permission
    /// the importer lacks, is left as a box with nothing set, and counted by class name; so is anything that used the
    /// variable it would have made.</summary>
    private static (int Count, Dictionary<string, int> Refused) SetUpWired(AppDbContext db,
        List<(RoomCopyFloorItem Copy, UserItem Item, WiredBoxType Type)> boxes,
        List<(RoomCopyFloorItem Copy, UserItem Item)> floor, List<(RoomCopyWallItem Copy, UserItem Item)> wall,
        IReadOnlyList<RoomCopyVariable> variables, IReadOnlySet<string> held)
    {
        var bySource = floor.Where(p => p.Copy.SourceId != 0).Select(p => (p.Copy.SourceId, p.Item))
            .Concat(wall.Where(p => p.Copy.SourceId != 0).Select(p => (p.Copy.SourceId, p.Item)))
            .GroupBy(p => p.SourceId)
            .ToDictionary(g => g.Key, g => g.First().Item);
        var floorById = floor.ToDictionary(p => p.Item.Id);

        // picks as a save keeps them: copied, each once, on the wall only where the dialog allows it
        List<int> Picks(WiredBoxType type, IReadOnlyList<long> sourceIds) => !type.TakesPicks ? [] : sourceIds
            .Select(id => bySource.GetValueOrDefault(id))
            .OfType<UserItem>()
            .Where(item => type.AllowsWallFurni || item.WallPosition is null)
            .Select(item => item.Id)
            .Distinct()
            .Take(WiredBoxType.MaxPickedFurni)
            .ToList();

        // a variable box's variable is named by its text, which Habbo keeps unique in a room
        var boxesByName = boxes
            .Where(b => b.Type is WiredVariableBox)
            .GroupBy(b => b.Copy.Wired!.StringParam, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Item.Id, StringComparer.OrdinalIgnoreCase);
        var variableIds = CopiedVariables.Map(variables, boxesByName);

        var settled = new List<(FurniWired Settings, WiredBoxType Type, string ClassName)>();
        var refusedIds = new HashSet<string>();
        var refused = new Dictionary<string, int>();
        void Refuse(UserItem item, WiredBoxType type, string className)
        {
            refused[className] = refused.GetValueOrDefault(className) + 1;
            if (type is WiredVariableBox)
                refusedIds.Add(item.Id.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var (copy, item, type) in boxes)
        {
            var wired = copy.Wired!;
            var className = copy.ClassName ?? $"floor:{copy.ClassId}";
            var settings = new FurniWired
            {
                Item = item,
                IntParams = wired.IntParams.Take(WiredBoxType.MaxIntParams).ToList(),
                StringParam = Clip(wired.StringParam, FurniWired.StringParamMaxLength),
                StuffIds = Picks(type, wired.StuffIds),
                StuffIds2 = Picks(type, wired.StuffIds2),
                FurniSources = [.. wired.FurniSources],
                UserSources = [.. wired.UserSources],
                VariableIds = wired.VariableIds.Select(id => variableIds.GetValueOrDefault(id)).OfType<string>().ToList(),
                DelayInPulses = wired.DelayInPulses,
                Quantifier = wired.Quantifier,
                IsFilter = wired.IsFilter,
                IsInvert = wired.IsInvert,
            };
            if ((type.Permission is { } permission && !held.Contains(permission)) || RoomWired.Settle(type, settings) is not null)
            {
                Refuse(item, type, className);
                continue;
            }
            settled.Add((settings, type, className));
        }

        // a box using a variable whose box was refused, or a part of one, would point at nothing
        var count = 0;
        foreach (var (settings, type, className) in settled)
        {
            if (settings.VariableIds.Any(id => refusedIds.Contains(id.Split(WiredSubVariables.Separator)[0])))
            {
                Refuse(settings.Item!, type, className);
                continue;
            }

            if (type.TakesSnapshot)
            {
                foreach (var picked in settings.StuffIds.Where(floorById.ContainsKey))
                {
                    var (pickedCopy, pickedItem) = floorById[picked];
                    settings.Snapshots.Add(new FurniWiredSnapshot
                    {
                        Item = pickedItem,
                        State = Clip(pickedCopy.Data.State ?? "", FurniWiredSnapshot.StateMaxLength),
                        X = pickedItem.X ?? 0,
                        Y = pickedItem.Y ?? 0,
                        Z = pickedItem.Z ?? 0,
                        Rotation = pickedItem.Rotation ?? 0,
                    });
                }
            }
            db.FurniWiredBoxes.Add(settings);
            count++;
        }

        return (count, refused);
    }

    /// <summary>The copy's data as this hotel keeps it, in the same layout.</summary>
    private static FurniDataView View(RoomCopyItemData data) => new(
        data.Type,
        data.State,
        data.Map?.Select(entry => new FurniMapEntryView(entry.Key, entry.Value)).ToList(),
        data.Strings?.ToList(),
        data.VoteResult,
        data.Ints?.ToList(),
        data.ScoreType,
        data.ClearType,
        data.HighScores?.Select(score => new FurniHighScoreEntryView(score.Score, score.Users.ToList(), DateTimeOffset.UtcNow)).ToList(),
        data.Hits,
        data.Target,
        UniqueSerialNumber: null,
        UniqueSeriesSize: null);

    private static string Clip(string text, int length) => text.Length <= length ? text : text[..length];
}
