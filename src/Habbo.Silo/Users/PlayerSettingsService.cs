using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Customfilter;
using Habbo.Protocol.Messages.Outgoing.Discord;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Users;

/// <summary>A player's own chat filter (the client filters what it shows by it) and how they show on Discord.</summary>
public sealed class PlayerSettingsService(
    IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger, IOptions<PlayerSettingsOptions> options) : IPlayerSettingsService
{
    public async Task SendFilterAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var words = await db.UserFilterWords.Where(w => w.UserId == player.Id).OrderBy(w => w.Word).Select(w => w.Word).ToListAsync();
        await messenger.SendAsync(player.Id, new GetCustomFilterResultMessageComposer(words));
    }

    public async Task AddFilterWordAsync(PlayerData player, string word)
    {
        word = word.Trim().ToLowerInvariant();
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (word.Length == 0 || word.Length > Math.Min(rules.MaxFilterWordLength, UserFilterWord.WordMaxLength)
            || await db.UserFilterWords.CountAsync(w => w.UserId == player.Id) >= rules.MaxFilterWords
            || await db.UserFilterWords.AnyAsync(w => w.UserId == player.Id && w.Word == word))
            return;

        db.UserFilterWords.Add(new UserFilterWord { UserId = player.Id, Word = word });
        await db.SaveChangesAsync();
        await messenger.SendAsync(player.Id, new ModifyCustomFilterResultMessageComposer(ModifyCustomFilterResultMessageComposer.Added, word));
    }

    public async Task RemoveFilterWordAsync(PlayerData player, string word)
    {
        word = word.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserFilterWords.Where(w => w.UserId == player.Id && w.Word == word).ExecuteDeleteAsync() > 0)
            await messenger.SendAsync(player.Id, new ModifyCustomFilterResultMessageComposer(ModifyCustomFilterResultMessageComposer.Removed, word));
    }

    public async Task SendDiscordPreferencesAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var preferences = await db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == player.Id) ?? new UserPreferences { UserId = player.Id };
        await messenger.SendAsync(player.Id, new DiscordPreferencesComposer(preferences.DiscordVersion, preferences.DiscordShowHabbo,
            preferences.DiscordShareActivity, preferences.DiscordHideInHiddenRooms, preferences.DiscordAllowJoining));
    }

    public async Task SetDiscordPreferencesAsync(PlayerData player, int version, bool showHabbo, bool shareActivity, bool hideInHiddenRooms, bool allowJoining)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var preferences = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == player.Id);
        if (preferences is null)
            db.UserPreferences.Add(preferences = new UserPreferences { UserId = player.Id });

        (preferences.DiscordVersion, preferences.DiscordShowHabbo, preferences.DiscordShareActivity, preferences.DiscordHideInHiddenRooms,
            preferences.DiscordAllowJoining) = (version, showHabbo, shareActivity, hideInHiddenRooms, allowJoining);
        await db.SaveChangesAsync();
        await SendDiscordPreferencesAsync(player);
    }
}
