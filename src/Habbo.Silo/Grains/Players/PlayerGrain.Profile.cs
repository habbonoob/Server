using Habbo.Silo.Abstractions.Permissions;
using Habbo.Silo.Users;
using Habbo.Domain.Friends;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.ChatStyles;
using Habbo.Protocol.Messages.Outgoing.HotLooks;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Players;

/// <summary>What others see of a player: the profile, their friends by relationship, star gems;
/// and the account details only the player sees.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>The profile date format the client shows as it is.</summary>
    private const string CreationDateFormat = "yyyy-MM-dd";

    /// <summary>The profile's trailing fields the client reads and never shows.</summary>
    private const int UnusedProfileInteger = 0;

    private const bool UnusedProfileFlag = false;

    public async Task SendExtendedProfileAsync(int accountId, bool openProfileWindow)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (user is not null)
            await SendProfileAsync(db, user, openProfileWindow);
    }

    public async Task SendExtendedProfileByNameAsync(string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lower = name.ToLowerInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == lower);
        if (user is not null)
            await SendProfileAsync(db, user, openProfileWindow: true);
    }

    private async Task SendProfileAsync(AppDbContext db, User user, bool openProfileWindow)
    {
        var isSelf = user.Id == PlayerId;
        var hidden = user.ProfileHidden && !isSelf;
        var online = (await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key)
            .WhichAreOnlineAsync([user.Id])).Count > 0;
        var onlineHidden = (await OnlineVisibility.HiddenFromAsync(db, PlayerId, [user.Id])).Count > 0;

        var friendCount = await db.Friendships.CountAsync(f => f.UserId == user.Id);
        var isFriend = await db.Friendships.AnyAsync(f => f.UserId == PlayerId && f.FriendId == user.Id);
        var requestSent = await db.FriendRequests.AnyAsync(r => r.RequesterId == PlayerId && r.TargetId == user.Id);
        // the badges: how many, how many of each rarity, the achievement level they add up to, and the rank by count
        var codes = await db.UserBadges.AsNoTracking().Where(b => b.UserId == user.Id).Select(b => b.Code).Distinct().ToListAsync();
        var owners = await BadgeLeaderboards.OwnersAsync(db, codes);
        var byRarity = codes.GroupBy(c => badgeLeaderboards.Describe(owners.GetValueOrDefault(c)).Rarity)
            .ToDictionary(g => g.Key, g => g.Count());
        var rarityCounts = Enum.GetValues<BadgeRarity>()
            .Select(r => new BadgeRarityCountData(r, byRarity.GetValueOrDefault(r)))
            .ToList();
        var achievementLevel = await db.UserAchievements.Where(a => a.UserId == user.Id).SumAsync(a => a.Level);
        var badgesRank = await badgeLeaderboards.TotalBadgesRankAsync(user.Id);

        var lastAccess = user.LastLoginAt is { } login
            ? (int)Math.Max(0, (DateTimeOffset.UtcNow - login).TotalSeconds)
            : ExtendedProfileData.Unknown;

        Send(new ExtendedProfileMessageComposer(new ExtendedProfileData(
            user.AccountId,
            user.Username,
            user.Figure,
            user.Motto,
            user.CreatedAt.UtcDateTime.ToString(CreationDateFormat, System.Globalization.CultureInfo.InvariantCulture),
            user.AchievementScore,
            hidden ? ExtendedProfileData.Unknown : friendCount,
            isFriend,
            requestSent,
            online && !hidden && !onlineHidden,
            onlineHidden ? ExtendedProfileData.OnlineHidden : online && !hidden ? ExtendedProfileData.Online : ExtendedProfileData.Offline,
            await ProfileGroupsAsync(db, user.Id),
            // a last login a moment ago would say they are online, which hiding that is meant to keep from others
            hidden || onlineHidden ? ExtendedProfileData.Unknown : lastAccess,
            openProfileWindow,
            hidden,
            user.Level,
            UnusedProfileInteger,
            user.StarGems,
            UnusedProfileFlag,
            UnusedProfileFlag,
            codes.Count,
            achievementLevel,
            rarityCounts,
            badgesRank)));
    }

    /// <summary>The groups a profile lists.</summary>
    private static Task<List<HabboGroupEntryData>> ProfileGroupsAsync(AppDbContext db, Guid userId) =>
        Habbo.Silo.Groups.GroupService.GroupEntriesAsync(db, userId);

    public async Task SendRelationshipsAsync(int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (user is null)
            return;

        var friends = await db.Friendships.AsNoTracking()
            .Where(f => f.UserId == user.Id && f.Relationship != RelationshipStatus.None)
            .Select(f => new { f.Relationship, f.Friend!.AccountId, f.Friend.Username, f.Friend.Figure })
            .ToListAsync();

        Send(new RelationshipStatusInfoComposer(accountId, friends
            .GroupBy(f => f.Relationship)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var shown = g.ElementAt(Random.Shared.Next(g.Count()));
                return new RelationshipStatusInfoData((int)g.Key, g.Count(), shown.AccountId, shown.Username, shown.Figure);
            })
            .ToList()));
    }

    public async Task GiveStarGemsAsync(int accountId, int amount)
    {
        if (amount < 1)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var receiver = await db.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (receiver is null || receiver.Id == PlayerId)
            return;

        var since = DateTimeOffset.UtcNow.Date;
        var given = await db.StarGemGifts.Where(g => g.GiverId == PlayerId && g.GivenAt >= since).SumAsync(g => g.Amount);
        var allowed = Math.Min(amount, accountOptions.Value.StarGemsPerDay - given);
        if (allowed < 1)
            return;

        // added in the database, not written back as a total: two players giving at once would otherwise lose one's gems
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Users.Where(u => u.Id == receiver.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.StarGems, u => u.StarGems + allowed));
            db.StarGemGifts.Add(new StarGemGift { GiverId = PlayerId, ReceiverId = receiver.Id, Amount = allowed });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // anyone showing their profile sees the new count
        Send(new ExtendedProfileChangedMessageComposer(accountId));
        // not waited on: two players giving each other gems at once would each wait on the other
        GrainFactory.GetGrain<IPlayerGrain>(receiver.Id).ReloadAsync().Ignore();
    }

    public async Task<PlayerData?> ReplenishRespectAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (RespectReplenishesLeft(user) < 1)
            return null;

        var cost = await RespectReplenishCostAsync();
        if (!await SpendActivityPointsAsync(DucketType, cost, CurrencyTransactionReasons.RespectReplenish))
            return null;

        user = await LoadUserAsync(db);
        user.RespectLeft = User.DailyRespects;
        user.RespectRefreshedOn = today;
        user.RespectReplenishes = user.RespectReplenishedOn == today ? user.RespectReplenishes + 1 : 1;
        user.RespectReplenishedOn = today;
        await db.SaveChangesAsync();
        return Remember(user);
    }

    /// <summary>The currency respects are bought back with: duckets.</summary>
    private const int DucketType = (int)Habbo.Domain.Catalog.ActivityPointType.Ducket;

    /// <summary>What the client tells the player a refill costs: its respect.replenish_cost_duckets.</summary>
    private const string RespectReplenishCostVariable = "respect.replenish_cost_duckets";

    private async Task<int> RespectReplenishCostAsync() =>
        int.TryParse(await clientVariables.GetAsync(RespectReplenishCostVariable), out var cost)
            ? cost
            : accountOptions.Value.RespectReplenishCostFallback;

    public async Task SendEmailStatusAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);
        Send(new EmailStatusResultComposer(user.Email, user.EmailVerified, user.EmailChangeCount < accountOptions.Value.EmailMaxChanges));
    }

    public async Task ChangeEmailAsync(string email)
    {
        var options = accountOptions.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await LoadUserAsync(db);

        var result = EmailChangeResults.Changed;
        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
            result = EmailChangeResults.Invalid;
        else if (email.Length > Math.Min(options.EmailMaxLength, User.EmailMaxLength))
            result = EmailChangeResults.TooLong;
        else if (user.EmailChangeCount >= options.EmailMaxChanges)
            result = EmailChangeResults.TooManyChanges;
        else if (await db.Users.AnyAsync(u => u.Id != PlayerId && u.Email.ToLower() == email.ToLower()))
            result = EmailChangeResults.InUse;

        if (result == EmailChangeResults.Changed)
        {
            user.Email = email;
            user.EmailVerified = false;
            user.EmailChangeCount++;
            await db.SaveChangesAsync();
        }

        Send(new ChangeEmailResultComposer(result));
        if (result == EmailChangeResults.Changed)
            await SendEmailStatusAsync();
    }

    public Task SendMessagesOfTheDayAsync()
    {
        var messages = accountOptions.Value.MessagesOfTheDay;
        if (messages.Count > 0)
            Send(new MOTDNotificationComposer(messages));
        return Task.CompletedTask;
    }

    public async Task SendHotLooksAsync(int amount)
    {
        var options = accountOptions.Value;
        var since = DateTimeOffset.UtcNow.AddDays(-options.HotLooksRecentDays);
        await using var db = await dbFactory.CreateDbContextAsync();
        var looks = await db.Users.AsNoTracking()
            .Where(u => u.Id != PlayerId && !u.ProfileHidden && u.LastLoginAt >= since)
            .Select(u => new { u.Figure, u.Gender })
            .Distinct()
            .OrderBy(_ => EF.Functions.Random())
            .Take(Math.Clamp(amount, 0, options.HotLooksMaxAmount))
            .ToListAsync();

        Send(new HotLooksMessageComposer(looks.Select(l => new HotLookData(l.Gender.ToLetter(), l.Figure)).ToList()));
    }

    public async Task SendChatStylesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await db.UserChatStyles.AsNoTracking()
            .Where(s => s.UserId == PlayerId)
            .OrderBy(s => s.StyleId)
            .Select(s => s.StyleId)
            .ToListAsync();
        Send(new UserPurchasableChatStylesMessageComposer(owned));
    }

    /// <summary>The client's list of styles it keeps out of the selector.</summary>
    private const string DisabledChatStylesVariable = "disabled.custom.chat.styles";

    /// <summary>The same rules the client's chat style selector applies.</summary>
    public async Task<bool> CanUseChatStyleAsync(int styleId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var style = await db.ChatStyles.AsNoTracking().FirstOrDefaultAsync(s => s.Id == styleId);
        if (style is null || style.SystemStyle)
            return false;

        var user = await LoadUserAsync(db);
        var owned = await db.UserChatStyles.AnyAsync(s => s.UserId == PlayerId && s.StyleId == styleId);
        if (style.IsNft || style.Id >= Habbo.Domain.Chat.ChatStyle.FirstNftStyle)
            return owned;

        if (style.Purchasable)
            return owned;

        var isStaff = permissions.Has(user, PermissionNames.ChatStaffStyles);
        if (style.StaffOverrideable && isStaff)
            return true;
        if (style.AmbassadorOnly)
            return isStaff || user.IsAmbassador;

        var disabled = (await clientVariables.GetAsync(DisabledChatStylesVariable) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (disabled.Contains(styleId.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            return owned;

        return style.HcOnly ? user.ClubLevel > ClubLevel.None || owned : true;
    }
}
