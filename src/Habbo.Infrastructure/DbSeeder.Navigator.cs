using Habbo.Domain.Navigator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The official rooms view, room networks, and the rooms links forward to.</summary>
public static partial class DbSeeder
{
    private sealed record SeedOfficialRooms(
        List<SeedOfficialEntry>? Entries,
        List<SeedOfficialEntry>? Ads,
        List<SeedRoomNetwork>? Networks,
        Dictionary<string, string>? ForwardRooms,
        List<SeedPromotedCategory>? PromotedCategories,
        List<SeedLiftedRoom>? LiftedRooms);

    private sealed record SeedLiftedRoom(string Room, int AreaId, string? Image, string? Caption);

    private sealed record SeedPromotedCategory(string Code, List<string> Rooms);

    private sealed record SeedOfficialEntry(
        OfficialRoomEntryType Type,
        string? RoomName,
        string? Tag,
        string? PopupCaption,
        string? PopupDescription,
        bool ShowDetails,
        string? PicText,
        string? PicRef,
        bool Open,
        List<SeedOfficialEntry>? Children);

    private sealed record SeedRoomNetwork(int Id, string Name, List<string> Rooms);

    /// <summary>seed/official_rooms.json. Rooms are named, as the rooms seed names them. Each part
    /// is seeded only while its table is empty, so a hotel's own arrangement is kept.</summary>
    private static async Task SeedOfficialRoomsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var seed = await seedFiles.ReadJsonAsync<SeedOfficialRooms>("official_rooms.json", ct);
        if (seed is null)
            return;

        var roomIds = await db.Rooms.GroupBy(r => r.Name).Select(g => new { g.Key, Id = g.Min(r => r.Id) })
            .ToDictionaryAsync(r => r.Key, r => r.Id, ct);
        int? RoomOf(string? name)
        {
            if (name is null)
                return null;
            if (roomIds.TryGetValue(name, out var id))
                return id;
            logger.LogWarning("official_rooms.json names room '{Room}', which does not exist.", name);
            return null;
        }

        if (!await db.OfficialRoomEntries.AnyAsync(ct))
        {
            var order = 0;
            async Task AddAsync(IEnumerable<SeedOfficialEntry> entries, int? folderId, bool isAd)
            {
                foreach (var entry in entries)
                {
                    var row = new OfficialRoomEntry
                    {
                        FolderId = folderId,
                        SortOrder = order++,
                        Type = entry.Type,
                        PopupCaption = entry.PopupCaption ?? string.Empty,
                        PopupDescription = entry.PopupDescription ?? string.Empty,
                        ShowDetails = entry.ShowDetails,
                        PicText = entry.PicText ?? string.Empty,
                        PicRef = entry.PicRef ?? string.Empty,
                        Tag = entry.Tag,
                        RoomId = RoomOf(entry.RoomName),
                        Open = entry.Open,
                        IsAd = isAd,
                    };
                    if (row.Type == OfficialRoomEntryType.Room && row.RoomId is null)
                        continue;

                    db.OfficialRoomEntries.Add(row);
                    await db.SaveChangesAsync(ct);
                    await AddAsync(entry.Children ?? [], row.Id, isAd);
                }
            }

            await AddAsync(seed.Entries ?? [], null, isAd: false);
            await AddAsync(seed.Ads ?? [], null, isAd: true);
            logger.LogInformation("Seeded {Count} official room entr(ies).", order);
        }

        if (!await db.RoomNetworks.AnyAsync(ct))
        {
            foreach (var network in seed.Networks ?? [])
                db.RoomNetworks.Add(new RoomNetwork
                {
                    Id = network.Id,
                    Name = network.Name,
                    RoomIds = network.Rooms.Select(RoomOf).OfType<int>().ToList(),
                });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PromotedRoomCategories.AnyAsync(ct))
        {
            var order = 0;
            foreach (var category in seed.PromotedCategories ?? [])
                db.PromotedRoomCategories.Add(new PromotedRoomCategory
                {
                    Code = category.Code,
                    SortOrder = order++,
                    RoomIds = category.Rooms.Select(RoomOf).OfType<int>().ToList(),
                });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LiftedRooms.AnyAsync(ct))
        {
            var order = 0;
            foreach (var lifted in seed.LiftedRooms ?? [])
                if (RoomOf(lifted.Room) is { } roomId)
                    db.LiftedRooms.Add(new LiftedRoom
                    {
                        RoomId = roomId, AreaId = lifted.AreaId, Image = lifted.Image ?? string.Empty,
                        Caption = lifted.Caption ?? string.Empty, SortOrder = order++,
                    });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ForwardRooms.AnyAsync(ct))
        {
            foreach (var (type, roomName) in seed.ForwardRooms ?? [])
                if (RoomOf(roomName) is { } roomId)
                    db.ForwardRooms.Add(new ForwardRoom { Type = type, RoomId = roomId });
            await db.SaveChangesAsync(ct);
        }
    }
}
