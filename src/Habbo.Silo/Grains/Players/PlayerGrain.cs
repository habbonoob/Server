using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Players;

/// <summary>Holds a player's live state. In-memory for now; persistence provider decision comes with RoomGrain.</summary>
public sealed partial class PlayerGrain(
    IDbContextFactory<AppDbContext> dbFactory,
    FurniInteractionRegistry interactions,
    FurniDataLoader furniData,
    IClientPacketSink sink,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Users.AccountOptions> accountOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Subscriptions.ClubOptions> clubOptions,
    Habbo.Silo.Users.FigureValidator figures,
    IClientVariables clientVariables,
    IShopCatalog shopCatalog,
    IAchievementService achievements,
    ISubscriptionService subscriptions,
    Microsoft.Extensions.Options.IOptions<CatalogExtrasOptions> catalogExtras,
    IGiftWrappingService giftWrapping,
    Microsoft.Extensions.Options.IOptions<GiftWrappingOptions> giftWrappingOptions,
    Habbo.Silo.Chat.WordFilter wordFilter,
    INameRules nameRules,
    Habbo.Silo.Pets.PetNames petNames,
    Habbo.Silo.Pets.PetRules petRules,
    Habbo.Silo.Friends.FriendActivityNotifier friendActivity,
    Habbo.Silo.Abstractions.Friends.IFriendListService friendList,
    Habbo.Silo.Achievements.AchievementResolutions resolutions,
    Habbo.Silo.Users.BadgeLeaderboards badgeLeaderboards,
    Habbo.Silo.Permissions.PermissionService permissions,
    IEarningsService earnings,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Users.EarningsOptions> earningsOptions,
    ILogger<PlayerGrain> logger) : Grain, IPlayerGrain
{

    private PlayerData? _cached;

    /// <summary>The account as last loaded. Each change reaches the copy the player's sessions hold too, which
    /// the gateway hands to the services it calls: a trade lock, a new rank or a lapsed membership counts at once.</summary>
    private PlayerData? Data
    {
        get => _cached;
        set
        {
            _cached = value;
            if (value is not null)
                foreach (var session in _sessions)
                    sink.RefreshPlayer(session, value);
        }
    }

    /// <summary>The hotel's one guide desk.</summary>
    private const long GuideDesk = 0;

    /// <summary>The room the player is in, as the gateway last told us.</summary>
    private int? _roomId;
    private readonly HashSet<Guid> _sessions = [];

    /// <summary>Whether the player's friends were last told they are in a room, and the announcement on its way.</summary>
    private bool _friendsSeeInRoom;
    private IGrainTimer? _roomAnnouncement;

    /// <summary>How long a change of room waits before the friends hear of it: going from one room to the next is
    /// out of the first and into the second, which tells them nothing.</summary>
    private static readonly TimeSpan RoomAnnouncementDelay = TimeSpan.FromSeconds(1);

    /// <summary>Keeps the grain up while the player is online: its sessions and room live only here, so a grain
    /// collected while idle would forget them, and messages, kicks and bans would reach no one.</summary>
    private IGrainTimer? _online;

    /// <summary>How often the online timer fires; besides keeping the grain up it ends a membership that ran out.</summary>
    private static readonly TimeSpan OnlineHeartbeat = TimeSpan.FromMinutes(5);

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        DelayDeactivation(TimeSpan.FromMinutes(10));
        return Task.CompletedTask;
    }

    public Task SendAsync(ComposedMessage message)
    {
        foreach (var session in _sessions)
            sink.TrySend(session, message);
        return Task.CompletedTask;
    }

    public async Task<PlayerData> GetDataAsync()
    {
        if (Data is not null)
            return Data;

        await using var db = await dbFactory.CreateDbContextAsync();
        Data = ToPlayerData(await LoadUserAsync(db));
        return Data;
    }

    public async Task SetOnlineAsync(Guid sessionId)
    {
        // one login at a time: whoever was logged in already is told they logged in elsewhere
        foreach (var earlier in _sessions.Where(s => s != sessionId).ToList())
        {
            DisconnectSession(earlier, Habbo.Protocol.Messages.Outgoing.Handshake.DisconnectReasonComposer.ConcurrentLogin);
            _sessions.Remove(earlier);
        }

        var loggingIn = _sessions.Count == 0;
        _sessions.Add(sessionId);
        _online ??= this.RegisterGrainTimer(async _ =>
            {
                await ExpireLapsedMembershipAsync();
                await OnlineAchievementsAsync();
            },
            new GrainTimerCreationOptions { DueTime = OnlineHeartbeat, Period = OnlineHeartbeat, KeepAlive = true });
        if (loggingIn)
        {
            await LoginAchievementsAsync();
            await DailyGiftAsync();
        }
        await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).AddAsync(this.GetPrimaryKey());
        logger.LogInformation("Player {PlayerId} is online ({Sessions} session(s))", this.GetPrimaryKey(), _sessions.Count);
    }

    public async Task<bool> SetOfflineAsync(Guid sessionId)
    {
        // the end of a session a second login replaced changes nothing: the newer one is still here
        _sessions.Remove(sessionId);
        if (_sessions.Count > 0)
            return false;

        _roomId = null;
        _online?.Dispose();
        _online = null;
        // signing out tells the friends everything
        _roomAnnouncement?.Dispose();
        _roomAnnouncement = null;
        _friendsSeeInRoom = false;

        await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).RemoveAsync(this.GetPrimaryKey());
        await GrainFactory.GetGrain<IGuideDeskGrain>(GuideDesk).PlayerLeftAsync(this.GetPrimaryKey());
        return true;
    }

    public async Task<int> SetHomeRoomAsync(int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(this.GetPrimaryKey());
        if (user is null)
            throw new InvalidOperationException($"Player {this.GetPrimaryKey()} does not exist.");

        if (await db.Rooms.AnyAsync(r => r.Id == roomId))
        {
            user.HomeRoomId = roomId;
            await db.SaveChangesAsync();
        }

        var data = ToPlayerData(user);
        Data = data;
        return data.HomeRoomId;
    }

    public async Task<AccountPreferences> GetPreferencesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return ToAccountPreferences(await LoadPreferencesAsync(db));
    }

    public async Task UpdatePreferencesAsync(PreferencesUpdate update)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var preferences = await LoadPreferencesAsync(db);

        preferences.UiVolume = Volume(update.UiVolume) ?? preferences.UiVolume;
        preferences.FurniVolume = Volume(update.FurniVolume) ?? preferences.FurniVolume;
        preferences.TraxVolume = Volume(update.TraxVolume) ?? preferences.TraxVolume;
        preferences.FreeFlowChatDisabled = update.FreeFlowChatDisabled ?? preferences.FreeFlowChatDisabled;
        preferences.RoomInvitesIgnored = update.RoomInvitesIgnored ?? preferences.RoomInvitesIgnored;
        preferences.RoomCameraFollowDisabled = update.RoomCameraFollowDisabled ?? preferences.RoomCameraFollowDisabled;
        preferences.PreferedChatStyle = update.PreferedChatStyle ?? preferences.PreferedChatStyle;
        preferences.UiFlags = update.UiFlags ?? preferences.UiFlags;
        preferences.CatalogNewAdditionsSeenAt = update.CatalogNewAdditionsSeenAt ?? preferences.CatalogNewAdditionsSeenAt;
        preferences.WiredMenuButton = update.WiredMenuButton ?? preferences.WiredMenuButton;
        preferences.WiredInspectButton = update.WiredInspectButton ?? preferences.WiredInspectButton;
        preferences.PlayTestMode = update.PlayTestMode ?? preferences.PlayTestMode;
        preferences.WiredWhisperDisabled = update.WiredWhisperDisabled ?? preferences.WiredWhisperDisabled;
        preferences.ShowAllNotifications = update.ShowAllNotifications ?? preferences.ShowAllNotifications;
        preferences.WiredUiStyle = update.WiredUiStyle is { } style
            ? style[..Math.Min(style.Length, UserPreferences.WiredUiStyleMaxLength)]
            : preferences.WiredUiStyle;
        preferences.ChatFontSize = Choice(update.ChatFontSize, UserPreferences.ChatFontSizeMax) ?? preferences.ChatFontSize;
        preferences.ChatMode = Choice(update.ChatMode, UserPreferences.ChatModeMax) ?? preferences.ChatMode;
        preferences.ChatBubbleWidth = Choice(update.ChatBubbleWidth, UserPreferences.ChatBubbleWidthMax) ?? preferences.ChatBubbleWidth;
        preferences.ChatScrollSpeed = Choice(update.ChatScrollSpeed, UserPreferences.ChatScrollSpeedMax) ?? preferences.ChatScrollSpeed;
        preferences.OnlineIndicator = update.OnlineIndicator is { } indicator && Enum.IsDefined(indicator)
            ? indicator
            : preferences.OnlineIndicator;
        await db.SaveChangesAsync();
    }

    /// <summary>The client's volume sliders run 0-100.</summary>
    private static int? Volume(int? value) => value is null ? null : Math.Clamp(value.Value, 0, 100);

    /// <summary>One of a setting's choices, 0 to <paramref name="max"/>; anything else changes nothing.</summary>
    private static int? Choice(int? value, int max) => value is { } v && v >= 0 && v <= max ? v : null;

    /// <summary>Loads the preferences row, creating it with the entity defaults on first use.</summary>
    private async Task<UserPreferences> LoadPreferencesAsync(AppDbContext db)
    {
        var userId = this.GetPrimaryKey();
        var preferences = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (preferences is not null)
            return preferences;

        preferences = new UserPreferences { UserId = userId };
        db.UserPreferences.Add(preferences);
        await db.SaveChangesAsync();
        return preferences;
    }

    private static AccountPreferences ToAccountPreferences(UserPreferences p) => new(
        p.UiVolume,
        p.FurniVolume,
        p.TraxVolume,
        p.FreeFlowChatDisabled,
        p.RoomInvitesIgnored,
        p.RoomCameraFollowDisabled,
        p.UiFlags,
        p.PreferedChatStyle,
        p.WiredMenuButton,
        p.WiredInspectButton,
        p.PlayTestMode,
        p.WiredWhisperDisabled,
        p.ShowAllNotifications,
        p.WiredUiStyle,
        p.InfoFeedEnabled,
        p.ChatFontSize,
        p.ChatMode,
        p.ChatBubbleWidth,
        p.ChatScrollSpeed,
        p.OnlineIndicator);

    public async Task<List<InventoryItemData>> GetInventoryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // Projected in memory rather than in SQL so the wire view is built by the same code
        // the room's pick-up path uses; an inventory is small enough that it costs nothing.
        // Only what the player is actually holding. A placed item belongs to the room and is
        // sent with it; listing it here as well put the same item in two places at once, and
        // the client will happily let you place it a second time.
        var items = await db.UserItems
            .AsNoTracking()
            .Include(i => i.Definition)
            .Where(i => i.OwnerId == this.GetPrimaryKey() && i.RoomId == null && !i.InMarketplace && i.ChestId == null)
            .OrderBy(i => i.Id)
            .ToListAsync();

        var data = await furniData.LoadAsync(db, items);

        return items
            .Where(item => item.Definition is not null)
            .Select(item => InventoryProjection.From(item, item.Definition!, data[item.Id]))
            .ToList();
    }

    public async Task<bool> SpendRespectAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == this.GetPrimaryKey()) is not { } user)
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (user.RespectRefreshedOn != today)
        {
            user.RespectLeft = User.DailyRespects;
            user.PetRespectLeft = User.DailyRespects;
            user.RespectRefreshedOn = today;
        }

        if (user.RespectLeft <= 0)
        {
            await db.SaveChangesAsync();
            return false;
        }

        user.RespectLeft--;
        await db.SaveChangesAsync();
        Data = Data is null ? null : Data with { RespectLeft = user.RespectLeft, PetRespectLeft = user.PetRespectLeft };
        return true;
    }

    public async Task<bool> SpendPetRespectAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == this.GetPrimaryKey()) is not { } user)
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (user.RespectRefreshedOn != today)
        {
            user.RespectLeft = User.DailyRespects;
            user.PetRespectLeft = User.DailyRespects;
            user.RespectRefreshedOn = today;
        }

        if (user.PetRespectLeft <= 0)
        {
            await db.SaveChangesAsync();
            return false;
        }

        user.PetRespectLeft--;
        await db.SaveChangesAsync();
        Data = Data is null ? null : Data with { RespectLeft = user.RespectLeft, PetRespectLeft = user.PetRespectLeft };
        return true;
    }

    public async Task<int> ReceiveRespectAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == this.GetPrimaryKey()) is not { } user)
            return 0;

        user.RespectTotal++;
        await db.SaveChangesAsync();
        Data = Data is null ? null : Data with { RespectTotal = user.RespectTotal };
        return user.RespectTotal;
    }

    public async Task<int> AddCreditsAsync(int credits, string reason)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == this.GetPrimaryKey());
        if (user is null)
            return 0;

        // a negative amount (a staff command taking credits) takes no more than there is, as activity points do
        var change = Math.Max(credits, -user.Credits);
        user.Credits += change;
        RecordTransaction(db, CurrencyTransaction.Credits, change, reason);
        await db.SaveChangesAsync();

        // The cached view carries the balance the hotel view already rendered.
        Data = Data is null ? null : Data with { Credits = user.Credits };

        logger.LogInformation("Player {PlayerId} was paid {Credits} credits; balance is now {Balance}.",
            this.GetPrimaryKey(), credits, user.Credits);

        return user.Credits;
    }

    /// <summary>Projects the stored account into the wire-facing player view.</summary>
    private PlayerData ToPlayerData(User user) => new(
        user.Id,
        user.AccountId,
        user.Username,
        user.Figure,
        user.Gender,
        user.Motto,
        user.Credits,
        user.Rank,
        user.RealName,
        user.DirectMail,
        user.RespectTotal,
        user.RespectLeft,
        user.PetRespectLeft,
        user.StreamPublishingAllowed,
        user.NameChangeAllowed,
        user.AccountSafetyLocked,
        user.ClubLevel,
        user.IsAmbassador,
        user.AchievementScore,
        user.IsModerator,
        user.LastLoginAt ?? user.CreatedAt,
        user.HomeRoomId ?? PlayerData.NoHomeRoom,
        RespectReplenishesLeft(user),
        user.TradeLocked || user.TradeLockedUntil > DateTimeOffset.UtcNow,
        user.NameColor,
        user.MutedUntil > DateTimeOffset.UtcNow ? user.MutedUntil : null,
        user.CreatedAt);

    /// <summary>How many respect refills are left today.</summary>
    private int RespectReplenishesLeft(User user) =>
        Math.Max(0, accountOptions.Value.RespectReplenishesPerDay
                    - (user.RespectReplenishedOn == DateOnly.FromDateTime(DateTime.UtcNow) ? user.RespectReplenishes : 0));
}
