using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Rooms;
using Habbo.Protocol.Composers;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's settings, and what its owner and those with rights may do to its visitors:
/// kick, mute and ban them, filter their words and hush the room.</summary>
public sealed partial class RoomGrain
{
    // RoomSettingsSaveError's codes, as the settings window words them
    private const int PasswordMandatory = 5;
    private const int NameMandatory = 7;
    private const int NameUnacceptable = 8;
    private const int DescriptionUnacceptable = 10;
    private const int TagUnacceptable = 11;
    private const int TagNotChoosable = 12;
    private const int TagTooLong = 13;

    // who may: the room's moderation settings (0 the owner, 1 those with rights, 2 everyone)
    private const int OwnerOnly = 0;
    private const int RightsHolders = 1;

    /// <summary>The notification an ambassador's alert shows its target.</summary>
    private const string AmbassadorAlertNotification = "ambassador.alert.warning";

    /// <summary>The client's yes and no in the settings' integer flags.</summary>
    private const int Yes = 1;
    private const int No = 0;

    /// <summary>Who is muted here, until when.</summary>
    private readonly Dictionary<Guid, DateTimeOffset> _roomMutes = [];

    /// <summary>The words the room's own filter replaces.</summary>
    private readonly List<string> _filterWords = [];

    private async Task LoadRoomRulesAsync(Habbo.Infrastructure.AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var mute in await db.RoomMutes.AsNoTracking().Where(m => m.RoomId == _room!.Id && m.ExpiresAt > now).ToListAsync())
            _roomMutes[mute.UserId] = mute.ExpiresAt;
        _filterWords.AddRange(await db.RoomFilterWords.AsNoTracking().Where(w => w.RoomId == _room!.Id).Select(w => w.Word).ToListAsync());
    }

    private bool IsOwnerOrStaffHere(Guid playerId) =>
        _room is not null && (_room.OwnerId == playerId || _entities?.Find(playerId) is { ControllerLevel: RoomControllerLevels.Staff });

    /// <summary>Whether the player may act on others by the room's setting for it.</summary>
    private bool MayModerate(Guid playerId, int setting) =>
        IsOwnerOrStaffHere(playerId)
        || (setting >= RightsHolders && _entities?.Find(playerId) is { ControllerLevel: >= RoomControllerLevels.Rights })
        || (setting > RightsHolders && _entities?.Find(playerId) is not null);

    /// <summary>The owner and staff are beyond the reach of the room's own moderation.</summary>
    private bool IsUntouchable(RoomEntityStore.RoomEntity target) =>
        target.Player.Id == _room!.OwnerId || permissions.Has(target.Player, PermissionNames.RoomStaffImmunity);

    /// <summary>The owner and staff manage a room, from inside it or from the navigator.</summary>
    private bool MayManage(PlayerData player) =>
        _room is not null && (_room.OwnerId == player.Id || permissions.Has(player, PermissionNames.RoomManageAny));

    public Task SendSettingsAsync(PlayerData player)
    {
        var playerId = player.Id;
        if (_room is null || !MayManage(player))
            return Task.CompletedTask;

        SendToPlayer(playerId, new RoomSettingsDataComposer(
            _room.Id,
            _room.Name,
            _room.Description,
            (int)_room.State,
            _room.CategoryId,
            _room.MaxUsers,
            roomOptions.Value.MaxVisitorsLimit,
            _room.Tags,
            _room.TradeMode,
            _room.AllowPets ? Yes : No,
            _room.AllowFoodConsume ? Yes : No,
            _room.AllowWalkThrough ? Yes : No,
            _room.WallsHidden ? Yes : No,
            _room.WallThicknessExponent,
            _room.FloorThicknessExponent,
            ChatSettings(),
            _room.AllowNavigatorDynamicCategories,
            _room.LeaveOnDoorTile,
            _room.IdleSleepEnabled,
            IdleSleepSeconds,
            _room.IdleAutokickEnabled,
            IdleAutokickSeconds,
            _room.MuteAllPets,
            new RoomModerationSettingsData(_room.WhoCanMute, _room.WhoCanKick, _room.WhoCanBan),
            _room.HiddenByBuildersClub));
        return Task.CompletedTask;
    }

    private RoomChatSettingsData ChatSettings() => new(
        _room!.ChatMode, _room.ChatBubbleWidth, _room.ChatScrollSpeed, _room.ChatHearRange, _room.ChatFloodSensitivity);

    public async Task SaveSettingsAsync(PlayerData player, RoomSettingsUpdate update)
    {
        var playerId = player.Id;
        if (_room is null || !MayManage(player))
            return;

        var rules = roomOptions.Value;
        var name = update.Name.Trim();
        var description = update.Description.Trim();
        var tags = update.Tags.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().Take(rules.MaxTags).ToList();
        var state = Enum.IsDefined(typeof(RoomState), update.DoorMode) ? (RoomState)update.DoorMode : _room.State;

        (int Code, string Info)? error = null;
        if (name.Length == 0)
            error = (NameMandatory, string.Empty);
        else if (!await settingsRules.IsCleanAsync(name))
            error = (NameUnacceptable, string.Empty);
        else if (!await settingsRules.IsCleanAsync(description))
            error = (DescriptionUnacceptable, string.Empty);
        else if (tags.FirstOrDefault(t => t.Length > rules.TagMaxLength) is { } longTag)
            error = (TagTooLong, longTag);
        else if (!permissions.Has(player, PermissionNames.RoomReservedTags) && tags.FirstOrDefault(t => rules.ReservedTags.Contains(t)) is { } reserved)
            error = (TagNotChoosable, reserved);
        else if (await FirstUncleanAsync(tags) is { } badTag)
            error = (TagUnacceptable, badTag);
        else if (state == RoomState.Password && update.Password.Length == 0 && _room.Password.Length == 0)
            error = (PasswordMandatory, string.Empty);

        if (error is { } failure)
        {
            SendToPlayer(playerId, new RoomSettingsSaveErrorComposer(_room.Id, failure.Code, failure.Info));
            return;
        }

        // what the window offers, and nothing else: these go out to every visitor's client as they are
        _room.Name = settingsRules.Name(name);
        _room.Description = settingsRules.Description(description);
        if (state != _room.State)
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ReachAchievementAsync(AchievementNames.SelfModDoorModeSeen, 1).Ignore();
        _room.State = state;
        if (update.Password.Length > 0)
            _room.Password = update.Password.Length > Room.PasswordMaxLength ? update.Password[..Room.PasswordMaxLength] : update.Password;
        _room.MaxUsers = settingsRules.MaxVisitors(update.MaximumVisitors);
        if (await settingsRules.MayPickCategoryAsync(player, update.CategoryId))
            _room.CategoryId = update.CategoryId;
        _room.Tags = tags;
        _room.TradeMode = RoomSettingsRules.Choice(update.TradeMode, 0, RoomSettingsRules.TradeModeMax, _room.TradeMode);
        _room.AllowPets = update.AllowPets;
        _room.AllowFoodConsume = update.AllowFoodConsume;
        _room.AllowWalkThrough = update.AllowWalkThrough;
        _room.WallsHidden = update.HideWalls;
        _room.WallThicknessExponent = RoomSettingsRules.Choice(update.WallThickness, RoomSettingsRules.ThicknessMin, RoomSettingsRules.ThicknessMax, _room.WallThicknessExponent);
        _room.FloorThicknessExponent = RoomSettingsRules.Choice(update.FloorThickness, RoomSettingsRules.ThicknessMin, RoomSettingsRules.ThicknessMax, _room.FloorThicknessExponent);
        _room.WhoCanMute = RoomSettingsRules.Choice(update.WhoCanMute, 0, RoomSettingsRules.WhoCanMuteMax, _room.WhoCanMute);
        _room.WhoCanKick = RoomSettingsRules.Choice(update.WhoCanKick, 0, RoomSettingsRules.WhoCanKickMax, _room.WhoCanKick);
        _room.WhoCanBan = RoomSettingsRules.Choice(update.WhoCanBan, 0, RoomSettingsRules.WhoCanBanMax, _room.WhoCanBan);
        _room.ChatMode = RoomSettingsRules.Choice(update.ChatMode ?? _room.ChatMode, 0, RoomSettingsRules.ChatModeMax, _room.ChatMode);
        _room.ChatBubbleWidth = RoomSettingsRules.Choice(update.ChatBubbleSize ?? _room.ChatBubbleWidth, 0, RoomSettingsRules.ChatBubbleWidthMax, _room.ChatBubbleWidth);
        _room.ChatScrollSpeed = RoomSettingsRules.Choice(update.ChatScrollSpeed ?? _room.ChatScrollSpeed, 0, RoomSettingsRules.ChatScrollSpeedMax, _room.ChatScrollSpeed);
        _room.ChatHearRange = RoomSettingsRules.Choice(update.ChatHearRange ?? _room.ChatHearRange, 0, RoomSettingsRules.ChatHearRangeMax, _room.ChatHearRange);
        _room.ChatFloodSensitivity = RoomSettingsRules.Choice(update.ChatFloodSensitivity, 0, RoomSettingsRules.ChatFloodSensitivityMax, _room.ChatFloodSensitivity);
        _room.AllowNavigatorDynamicCategories = update.AllowNavigatorDynamicCategories ?? _room.AllowNavigatorDynamicCategories;
        _room.MuteAllPets = update.MuteAllPets ?? _room.MuteAllPets;
        if (player.ClubLevel >= ClubLevel.Club)
            SetVisitorBehaviour(update);
        await SaveRoomAsync();
        ApplyIdleTimes();

        SendToPlayer(playerId, new RoomSettingsSavedComposer(_room.Id));
        Broadcast(new RoomInfoUpdatedComposer(_room.Id));
        Broadcast(new RoomChatSettingsMessageComposer(ChatSettings()));
        Broadcast(RoomVisualizationSettingsComposer.From(Visualization(_room, _model!)));
    }

    /// <summary>How the room treats its visitors, as the club lets its owner set it: each timeout off (0), or within
    /// what the settings window accepts, with being sent out coming at least a while after falling asleep. Times the
    /// window would not have sent leave the room's as they are.</summary>
    private void SetVisitorBehaviour(RoomSettingsUpdate update)
    {
        _room!.LeaveOnDoorTile = update.LeaveOnDoorTile ?? _room.LeaveOnDoorTile;
        if (update.IdleSleepEnabled is not { } sleeps || update.IdleAutokickEnabled is not { } kicks)
            return;

        var sleepAfter = update.IdleSleepTimeoutSeconds ?? 0;
        var kickAfter = update.IdleAutokickTimeoutSeconds ?? 0;
        var valid = (!sleeps || sleepAfter is >= Room.IdleSleepTimeoutMin and <= Room.IdleSleepTimeoutMax)
                    && (!kicks || kickAfter is >= Room.IdleAutokickTimeoutMin and <= Room.IdleAutokickTimeoutMax)
                    && (!sleeps || !kicks || kickAfter >= sleepAfter + Room.IdleAutokickAfterSleepMin);
        if (!valid)
            return;

        _room.IdleSleepEnabled = sleeps;
        _room.IdleSleepTimeoutSeconds = sleeps ? sleepAfter : 0;
        _room.IdleAutokickEnabled = kicks;
        _room.IdleAutokickTimeoutSeconds = kicks ? kickAfter : 0;
    }

    /// <summary>How long a visitor stays idle here before falling asleep, and before being sent out, in seconds: the
    /// room's own time, or the hotel's.</summary>
    private int IdleSleepSeconds =>
        _room!.IdleSleepTimeoutSeconds > 0 ? _room.IdleSleepTimeoutSeconds : roomOptions.Value.IdleSleepSeconds;

    private int IdleAutokickSeconds =>
        _room!.IdleAutokickTimeoutSeconds > 0 ? _room.IdleAutokickTimeoutSeconds : roomOptions.Value.IdleAutokickSeconds;

    /// <summary>Gives the avatars' clock the room's idle times, in ticks; a time that is off never comes.</summary>
    private void ApplyIdleTimes()
    {
        if (_entities is null || _room is null)
            return;

        _entities.SleepAfterTicks = _room.IdleSleepEnabled ? Ticks(IdleSleepSeconds) : RoomEntityStore.Never;
        _entities.SendOutAfterTicks = _room.IdleAutokickEnabled ? Ticks(IdleAutokickSeconds) : RoomEntityStore.Never;
    }

    private static int Ticks(int seconds) => (int)Math.Ceiling(seconds * 1000 / TickInterval.TotalMilliseconds);

    private async Task<string?> FirstUncleanAsync(IEnumerable<string> texts)
    {
        foreach (var text in texts)
        {
            if (!await wordFilter.IsCleanAsync(text))
                return text;
        }

        return null;
    }

    /// <summary>Writes the room's row from what the grain holds.</summary>
    private async Task SaveRoomAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var room = _room!;
        var row = await db.Rooms.FirstAsync(r => r.Id == room.Id);

        // the thumbnail is set from the navigator straight in the database: the room keeps what is there, not the
        // one it loaded, which would put an old thumbnail back on every save
        (room.ThumbnailBackgroundImageId, room.ThumbnailFrontImageId, room.ThumbnailForegroundImageId) =
            (row.ThumbnailBackgroundImageId, row.ThumbnailFrontImageId, row.ThumbnailForegroundImageId);
        db.Entry(row).CurrentValues.SetValues(room);
        row.Tags = room.Tags.ToList();
        await db.SaveChangesAsync();
    }

    public async Task UpdateCategoryAndTradeAsync(PlayerData player, int categoryId, int tradeMode)
    {
        if (_room is null || !MayManage(player))
            return;

        // the owner is asked again until the room has a category it may have
        if (!await settingsRules.MayPickCategoryAsync(player, categoryId))
        {
            SendToPlayer(player.Id, new ShowEnforceRoomCategoryDialogComposer(ShowEnforceRoomCategoryDialogComposer.AnyCategory));
            return;
        }

        _room.CategoryId = categoryId;
        _room.TradeMode = RoomSettingsRules.Choice(tradeMode, 0, RoomSettingsRules.TradeModeMax, _room.TradeMode);
        await SaveRoomAsync();
        Broadcast(new RoomInfoUpdatedComposer(_room.Id));
    }

    public async Task<bool> DeleteAsync(PlayerData player)
    {
        if (_room is null || _entities is null || _items is null || !MayManage(player))
            return false;

        foreach (var entity in _entities.Entities.ToList())
            await KickAsync(entity);
        await ReturnToOwnersAsync(_items.Items.ToList(), player.AccountId);
        await groups.RoomDeletedAsync(_room.Id);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.Rooms.Where(r => r.Id == _room.Id).ExecuteDeleteAsync();
            // nobody is sent home to a room that is gone: login would forward them into it
            await db.Users.Where(u => u.HomeRoomId == _room.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.HomeRoomId, (int?)null));
        }

        await GrainFactory.GetGrain<IRoomPopulationGrain>(0).SetAsync(_room.Id, 0, GrainContext.Address.SiloAddress);
        _room = null;
        DeactivateOnIdle();
        return true;
    }

    public Task SendFilterAsync(PlayerData player)
    {
        if (MayManage(player))
            SendToPlayer(player.Id, new RoomFilterSettingsMessageComposer(_filterWords.ToList()));
        return Task.CompletedTask;
    }

    public async Task UpdateFilterAsync(PlayerData player, bool add, string word)
    {
        word = word.Trim().ToLowerInvariant();
        if (_room is null || word.Length == 0 || word.Length > RoomFilterWord.WordMaxLength || !MayManage(player))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (add && !_filterWords.Contains(word) && _filterWords.Count < roomOptions.Value.FilterWordLimit)
        {
            _filterWords.Add(word);
            db.RoomFilterWords.Add(new RoomFilterWord { RoomId = _room.Id, Word = word });
        }
        else if (!add && _filterWords.Remove(word))
        {
            await db.RoomFilterWords.Where(w => w.RoomId == _room.Id && w.Word == word).ExecuteDeleteAsync();
        }

        await db.SaveChangesAsync();
        await SendFilterAsync(player);
    }

    public async Task KickUserAsync(Guid playerId, int accountId)
    {
        if (_room is null || !MayModerate(playerId, _room.WhoCanKick)
            || _entities?.Entities.FirstOrDefault(e => e.Player.AccountId == accountId) is not { } target
            || IsUntouchable(target) || target.Player.Id == playerId)
            return;

        await KickAsync(target);
    }

    public async Task BanUserAsync(Guid playerId, int accountId, string duration)
    {
        if (_room is null || !MayModerate(playerId, _room.WhoCanBan)
            || !roomOptions.Value.BanDurationsHours.TryGetValue(duration, out var hours))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (target is null || target.Id == _room.OwnerId || permissions.Has(target, PermissionNames.RoomStaffImmunity))
            return;

        var expires = DateTimeOffset.UtcNow.AddHours(hours);
        var ban = await db.RoomBans.FirstOrDefaultAsync(b => b.RoomId == _room.Id && b.UserId == target.Id);
        if (ban is null)
            db.RoomBans.Add(new RoomBan { RoomId = _room.Id, UserId = target.Id, ExpiresAt = expires });
        else
            ban.ExpiresAt = expires;
        await db.SaveChangesAsync();

        if (_entities?.Find(target.Id) is { } present)
            await KickAsync(present);
    }

    public async Task UnbanUserAsync(Guid playerId, int accountId)
    {
        if (_room is null || !MayModerate(playerId, _room.WhoCanBan))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var removed = await db.RoomBans.Where(b => b.RoomId == _room.Id && b.User!.AccountId == accountId).ExecuteDeleteAsync();
        if (removed > 0)
            SendToPlayer(playerId, new UserUnbannedFromRoomComposer(_room.Id, accountId));
    }

    public async Task SendBannedUsersAsync(Guid playerId)
    {
        if (_room is null || !MayModerate(playerId, _room.WhoCanBan))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var bans = await db.RoomBans.AsNoTracking()
            .Where(b => b.RoomId == _room.Id && b.ExpiresAt > DateTimeOffset.UtcNow)
            .Select(b => new BannedUserData(b.User!.AccountId, b.User.Username))
            .ToListAsync();
        SendToPlayer(playerId, new BannedUsersFromRoomComposer(_room.Id, bans));
    }

    public async Task MuteUserAsync(Guid playerId, int accountId, int minutes)
    {
        if (_room is null || minutes <= 0 || !MayModerate(playerId, _room.WhoCanMute)
            || _entities?.Entities.FirstOrDefault(e => e.Player.AccountId == accountId) is not { } target
            || IsUntouchable(target))
            return;

        minutes = Math.Min(minutes, LongestMuteMinutes);
        var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);
        _roomMutes[target.Player.Id] = expires;

        await using var db = await dbFactory.CreateDbContextAsync();
        var mute = await db.RoomMutes.FirstOrDefaultAsync(m => m.RoomId == _room.Id && m.UserId == target.Player.Id);
        if (mute is null)
            db.RoomMutes.Add(new RoomMute { RoomId = _room.Id, UserId = target.Player.Id, ExpiresAt = expires });
        else
            mute.ExpiresAt = expires;
        await db.SaveChangesAsync();

        SendTo(target.Player.Id, new RemainingMutePeriodComposer(minutes * SecondsPerMinute));
    }

    private const int SecondsPerMinute = 60;

    /// <summary>The longest mute the client offers (an ambassador's 72 hours); a longer one only a modified client sends.</summary>
    private const int LongestMuteMinutes = 72 * 60;

    public async Task UnmuteUserAsync(Guid playerId, int accountId)
    {
        if (_room is null || !MayModerate(playerId, _room.WhoCanMute)
            || _entities?.Entities.FirstOrDefault(e => e.Player.AccountId == accountId) is not { } target)
            return;

        _roomMutes.Remove(target.Player.Id);
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.RoomMutes.Where(m => m.RoomId == _room.Id && m.UserId == target.Player.Id).ExecuteDeleteAsync();
    }

    public async Task ToggleMuteAllAsync(Guid playerId)
    {
        if (_room is null || !IsOwnerOrStaffHere(playerId))
            return;

        _room.AllInRoomMuted = !_room.AllInRoomMuted;
        await SaveRoomAsync();
        Broadcast(new MuteAllInRoomComposer(_room.AllInRoomMuted));
    }

    public Task AmbassadorAlertAsync(Guid playerId, int accountId)
    {
        if (_entities?.Find(playerId) is not { } ambassador
            || (!ambassador.Player.IsAmbassador && !permissions.Has(ambassador.Player, PermissionNames.RoomAmbassadorAlert))
            || _entities.Entities.FirstOrDefault(e => e.Player.AccountId == accountId) is not { } target)
            return Task.CompletedTask;

        SendTo(target.Player.Id, new NotificationDialogMessageComposer(AmbassadorAlertNotification, []));
        return Task.CompletedTask;
    }

    /// <summary>Sends to a player in the room, or not (the settings window may be opened from the navigator).</summary>
    private void SendToPlayer(Guid playerId, IComposer composer)
    {
        if (_entities?.Find(playerId) is not null)
            SendTo(playerId, composer);
        else
            _ = GrainFactory.GetGrain<IPlayerGrain>(playerId).SendAsync(composer.ToMessage());
    }
}
