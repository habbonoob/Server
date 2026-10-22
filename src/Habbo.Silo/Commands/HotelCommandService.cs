using Habbo.Silo.Abstractions.Permissions;
using System.Globalization;
using Habbo.Domain.Chat;
using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Perk;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Chat;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Commands;

/// <summary>The staff's commands about the whole hotel: alerts, badges and gifts to many at once, ranks, happy hour,
/// offers, the word filter and what the server holds in memory. Each answers what to tell the staff member who ran it.</summary>
public sealed class HotelCommandService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    PermissionService permissions,
    CurrencyService currencies,
    IOptions<CurrencyOptions> currencyOptions,
    ITargetedOfferService offers,
    IPerkService perks,
    WordFilter wordFilter,
    IEnumerable<IReloadable> reloadables,
    IEnumerable<IReloadRelay> relays)
{
    /// <summary>The notification the client shows as a pop-up from the staff, with a link (:hal).</summary>
    private const string StaffAlertNotification = "admin.transient";

    /// <summary>What :update knows that is not held in memory: it is told to the client instead.</summary>
    private const string CatalogReloadName = "catalog";

    private const string GiftCardMessage = "A gift from the hotel staff.";

    private async Task<IReadOnlyCollection<Guid>> OnlineAsync() =>
        await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();

    private async Task<User?> FindUserAsync(string username)
    {
        if (username.Length == 0)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var name = username.ToLowerInvariant();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == name);
    }

    private static (string First, string Remainder) Split(string argument)
    {
        var parts = argument.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (parts.ElementAtOrDefault(0) ?? string.Empty, parts.ElementAtOrDefault(1)?.Trim() ?? string.Empty);
    }

    /// <summary>:ha &lt;message&gt; — the hotel's alert, to everyone online.</summary>
    public async Task<string> HotelAlertAsync(PlayerData staff, string argument)
    {
        var message = argument.Trim();
        if (message.Length == 0)
            return ":ha <message>";

        var online = await OnlineAsync();
        await messenger.SendAsync(online, new HabboBroadcastMessageComposer($"{message}\r\r- {staff.Username}"));
        return $"Sent to {online.Count} online.";
    }

    /// <summary>:hal &lt;url&gt; &lt;message&gt; — a pop-up from the staff to everyone online, with a link.</summary>
    public async Task<string> HotelAlertWithLinkAsync(PlayerData staff, string argument)
    {
        var (url, message) = Split(argument);
        if (url.Length == 0 || message.Length == 0)
            return ":hal <url> <message>";

        var online = await OnlineAsync();
        await messenger.SendAsync(online, new NotificationDialogMessageComposer(StaffAlertNotification,
        [
            new("message", $"{message}\r\r- {staff.Username}"),
            new("linkUrl", url),
        ]));
        return $"Sent to {online.Count} online.";
    }

    /// <summary>:sa &lt;message&gt; — an alert to the staff online: those who may use the moderation tool.</summary>
    public async Task<string> StaffAlertAsync(PlayerData staff, string argument)
    {
        var message = argument.Trim();
        if (message.Length == 0)
            return ":sa <message>";

        var holders = await StaffOnlineAsync();
        await messenger.SendAsync(holders.Select(u => u.Id), new HabboBroadcastMessageComposer($"Staff alert from {staff.Username}:\r{message}"));
        return $"Sent to {holders.Count} staff online.";
    }

    /// <summary>Who online may use the moderation tool.</summary>
    private async Task<IReadOnlyList<User>> StaffOnlineAsync()
    {
        var online = (await OnlineAsync()).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var users = await db.Users.AsNoTracking().Where(u => online.Contains(u.Id)).ToListAsync();
        return users.Where(u => permissions.Has(u, PermissionNames.ModerationTool)).OrderByDescending(u => u.Rank).ThenBy(u => u.Username).ToList();
    }

    /// <summary>:staffonline — the staff online, their rank and the room they are in.</summary>
    public async Task<string> ListStaffOnlineAsync()
    {
        var staff = await StaffOnlineAsync();
        if (staff.Count == 0)
            return "No staff are online.";

        await using var db = await dbFactory.CreateDbContextAsync();
        var lines = new List<string>();
        foreach (var user in staff)
        {
            var roomId = await grains.GetGrain<IPlayerGrain>(user.Id).GetCurrentRoomAsync();
            var room = roomId is { } id ? await db.Rooms.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefaultAsync() : null;
            lines.Add($"{user.Username} ({user.Rank}){(room is null ? "" : $" in {room}")}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>:givebadge / :takebadge &lt;username&gt; &lt;badge&gt;.</summary>
    public async Task<string> BadgeAsync(string argument, bool give)
    {
        var (name, code) = Split(argument);
        var usage = give ? ":givebadge <username> <badge>" : ":takebadge <username> <badge>";
        if (name.Length == 0 || code.Length == 0)
            return usage;
        if (await FindUserAsync(name) is not { } user)
            return $"No player is called {name}.";

        var player = grains.GetGrain<IPlayerGrain>(user.Id);
        return give
            ? await player.GiveBadgeAsync(code) ? $"{user.Username} has badge {code}." : $"{user.Username} already has badge {code}."
            : await player.RemoveBadgeAsync(code) ? $"{user.Username} no longer has badge {code}." : $"{user.Username} has no badge {code}.";
    }

    /// <summary>:roombadge / :massbadge &lt;badge&gt; — a badge to everyone given (those in the room, or online).</summary>
    public async Task<string> BadgeManyAsync(string argument, IReadOnlyCollection<Guid>? players)
    {
        var code = argument.Trim();
        if (code.Length == 0 || code.Contains(' '))
            return players is null ? ":massbadge <badge>" : ":roombadge <badge>";

        var given = 0;
        foreach (var playerId in players ?? await OnlineAsync())
            if (await grains.GetGrain<IPlayerGrain>(playerId).GiveBadgeAsync(code))
                given++;
        return $"Badge {code} given to {given}.";
    }

    /// <summary>:roomgift / :massgift &lt;item&gt; [message] — a present holding the item, by its number or its class
    /// name, to everyone given (those in the room, or online).</summary>
    public async Task<string> GiftManyAsync(PlayerData staff, string argument, IReadOnlyCollection<Guid>? players)
    {
        var (item, message) = Split(argument);
        var usage = players is null ? ":massgift <item> [message]" : ":roomgift <item> [message]";
        if (item.Length == 0)
            return usage;

        await using var db = await dbFactory.CreateDbContextAsync();
        var definition = int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id)
            : await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassName == item);
        if (definition is null)
            return $"There is no item {item}.";

        var given = 0;
        foreach (var playerId in players ?? await OnlineAsync())
            if (await grains.GetGrain<IPlayerGrain>(playerId).ReceivePrizeGiftAsync(definition.Id, message.Length > 0 ? message : GiftCardMessage, staff.Id))
                given++;
        return given == 0 ? "No gift could be wrapped: is there a free gift wrap?" : $"{definition.ClassName} given to {given}.";
    }

    /// <summary>:giverank &lt;username&gt; &lt;rank&gt; — a rank below one's own, by its number or name; the player
    /// sees what it lets them do at once.</summary>
    public async Task<string> GiveRankAsync(PlayerData staff, string argument)
    {
        var (name, rankText) = Split(argument);
        if (name.Length == 0 || !Enum.TryParse<SecurityLevel>(rankText, ignoreCase: true, out var rank) || !Enum.IsDefined(rank))
            return $":giverank <username> <rank> ({string.Join(", ", Enum.GetValues<SecurityLevel>().Select(l => $"{(int)l} {l}"))})";
        if (rank >= staff.Rank)
            return "You can only give ranks below your own.";

        await using var db = await dbFactory.CreateDbContextAsync();
        var lowered = name.ToLowerInvariant();
        if (await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == lowered) is not { } user)
            return $"No player is called {name}.";
        if (user.Rank >= staff.Rank)
            return $"{user.Username} is not below your rank.";

        user.Rank = rank;
        await db.SaveChangesAsync();

        var player = grains.GetGrain<IPlayerGrain>(user.Id);
        var data = await player.ReloadAsync();
        if ((await OnlineAsync()).Contains(user.Id))
        {
            await messenger.SendAsync(user.Id, UserRightsMessageComposer.From(data));
            await messenger.SendAsync(user.Id, PerkAllowancesMessageComposer.From(await perks.GetAllowancesAsync(data)));
            if (await player.GetCurrentRoomAsync() is { } roomId)
                await grains.GetGrain<IGameRoomGrain>(roomId).RefreshPlayerAsync(user.Id, data);
        }

        return $"{user.Username} is now {rank}.";
    }

    /// <summary>:happyhour [minutes] — the online payouts multiplied for a while (0 ends it), told to everyone.</summary>
    public async Task<string> HappyHourAsync(string argument)
    {
        var rules = currencyOptions.Value;
        var minutes = rules.HappyHourMinutes;
        if (argument.Trim().Length > 0 && (!int.TryParse(argument.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes) || minutes < 0))
            return ":happyhour [minutes]";

        if (currencies.SetHappyHour(minutes) is null)
            return "Happy hour is over.";

        var announcement = rules.HappyHourAnnouncement
            .Replace("{multiplier}", rules.HappyHourMultiplier.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{minutes}", minutes.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        await messenger.SendAsync(await OnlineAsync(), new HabboBroadcastMessageComposer(announcement));
        return $"Happy hour for {minutes} minutes.";
    }

    /// <summary>:promoteoffer &lt;offer id&gt; — a targeted offer shown to everyone online.</summary>
    public async Task<string> PromoteOfferAsync(string argument)
    {
        if (!int.TryParse(argument.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var offerId))
            return ":promoteoffer <offer id>";

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.TargetedOffers.AnyAsync(o => o.Id == offerId))
            return $"There is no offer {offerId}.";

        var online = await OnlineAsync();
        foreach (var playerId in online)
            await offers.SendAsync(await grains.GetGrain<IPlayerGrain>(playerId).GetDataAsync(), offerId);
        return $"Offer {offerId} shown to {online.Count} online.";
    }

    /// <summary>:filter &lt;word&gt; [replacement] — a word the filter replaces from now on.</summary>
    public async Task<string> FilterWordAsync(string argument)
    {
        var (word, replacement) = Split(argument);
        word = word.ToLowerInvariant();
        if (word.Length == 0 || word.Length > FilteredWord.WordMaxLength)
            return ":filter <word> [replacement]";

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.FilteredWords.FirstOrDefaultAsync(w => w.Word == word) is { } known)
            known.Replacement = replacement;
        else
            db.FilteredWords.Add(new FilteredWord { Word = word, Replacement = replacement });
        await db.SaveChangesAsync();
        // here, and on the other silos and gateways, as :update does
        wordFilter.Reload();
        foreach (var relay in relays)
            relay.Reload(wordFilter.ReloadName);
        return $"\"{word}\" is filtered{(replacement.Length > 0 ? $" as \"{replacement}\"" : "")}.";
    }

    /// <summary>:update &lt;name&gt; — reads again what the server holds in memory, or tells the clients the catalog
    /// changed.</summary>
    public async Task<string> UpdateAsync(string argument)
    {
        var name = argument.Trim().ToLowerInvariant();
        var known = reloadables.Select(r => r.ReloadName).Append(CatalogReloadName).Order(StringComparer.Ordinal).ToList();
        if (name == CatalogReloadName)
        {
            await messenger.SendAsync(await OnlineAsync(), new CatalogPublishedMessageComposer(true));
            return "The catalog is read again by everyone online.";
        }

        if (reloadables.FirstOrDefault(r => r.ReloadName == name) is not { } reloadable)
            return $":update <{string.Join(", ", known)}>";

        await reloadable.ReloadAsync();
        foreach (var relay in relays)
            relay.Reload(name);
        return $"{name} read again.";
    }
}
