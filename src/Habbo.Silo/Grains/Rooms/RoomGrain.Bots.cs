using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Inventory.Bots;
using Habbo.Protocol.Messages.Outgoing.Room.Action;
using Habbo.Protocol.Messages.Outgoing.Room.Bots;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Bots;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's bots: putting them down and picking them up, what their owners set them to do,
/// and doing it — chatting, walking about, dancing, serving drinks and keeping a log of visitors.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What separates the chatter settings, and the lines, in a bot's chatter configuration.</summary>
    private const string ChatterSeparator = ";#;";
    private const string ChatterLineSeparator = "\r";

    private const string UsernameToken = "%username%";
    private const string OwnerToken = "%owner%";
    private const string ItemToken = "%item%";
    private const string VisitorsToken = "%visitors%";
    private const string HandItemTextPrefix = "handitem";

    /// <summary>The skills only the bot's owner may use.</summary>
    private static readonly int[] OwnerSkills =
        [BotSkills.Figure, BotSkills.Chatter, BotSkills.RandomWalk, BotSkills.Dance, BotSkills.ChangeName];

    /// <summary>Worked out when asked: a static field here could be set before the tick interval it divides by.</summary>
    private static int TicksPerSecond => (int)(TimeSpan.FromSeconds(1) / TickInterval);

    private readonly Random _botRandom = new();

    /// <summary>The chat bubble bots talk in.</summary>
    private int _botChatStyle;

    private async Task LoadBotsAsync(AppDbContext db)
    {
        _botChatStyle = await db.ChatStyles.Where(s => s.AssetId == botOptions.Value.ChatStyleAsset).Select(s => s.Id).FirstOrDefaultAsync();
        var bots = await db.Bots.AsNoTracking().Include(b => b.Type).Include(b => b.Owner)
            .Where(b => b.RoomId == _room!.Id).ToListAsync();
        foreach (var bot in bots)
        {
            var (x, y) = OnFloor(bot.X, bot.Y);
            AddBot(new RoomBot { Data = bot, OwnerAccountId = bot.Owner!.AccountId, OwnerName = bot.Owner.Username }, x, y, bot.Rotation);
        }
    }

    /// <summary>Where a bot or pet stored at a tile is put when the room loads: there, or at the door when the floor plan
    /// changed under it since. Off the floor it would stand in the void, where it could never walk.</summary>
    private (int X, int Y) OnFloor(int x, int y) =>
        _model!.IsTile(x, y) ? (x, y) : (_model.DoorX, _model.DoorY);

    private RoomEntityStore.RoomEntity AddBot(RoomBot bot, int x, int y, int direction)
    {
        var entity = _entities!.AddBot(bot, x, y, _items!.AvatarHeightAt(x, y), direction);
        bot.NextChatTick = _entities.CurrentTick + ChatDelayTicks(bot);
        bot.NextWalkTick = _entities.CurrentTick;
        return entity;
    }

    public async Task PlaceBotAsync(Guid playerId, int botId, int x, int y)
    {
        if (_room is null || _model is null || _items is null || _entities?.Find(playerId) is not { } placer)
            return;

        var rules = botOptions.Value;
        var refusal = !rules.Enabled ? BotErrorComposer.ForbiddenInHotel
            : _room.OwnerId != playerId ? BotErrorComposer.ForbiddenInFlat
            : _entities.Bots.Count >= rules.MaxPerRoom ? BotErrorComposer.LimitReached
            : !_model.IsTile(x, y) || _items.BlocksWalking(x, y) || _entities.HasAvatarAt(x, y) ? BotErrorComposer.TileNotFree
            : (int?)null;
        if (refusal is { } error)
        {
            SendTo(playerId, new BotErrorComposer(error));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Bots.Include(b => b.Type).FirstOrDefaultAsync(b => b.Id == botId && b.OwnerId == playerId && b.RoomId == null) is not { } row)
            return;

        row.RoomId = _room.Id;
        row.X = x;
        row.Y = y;
        await db.SaveChangesAsync();

        var entity = AddBot(new RoomBot { Data = row, OwnerAccountId = placer.Player.AccountId, OwnerName = placer.Player.Username }, x, y, row.Rotation);
        Broadcast(UsersMessageComposer.From([_entities.ToWireData(entity)]));
        Broadcast(UserUpdateMessageComposer.From([_entities.LookTo(entity, placer.X, placer.Y) ?? _entities.CurrentStatus(entity)]));
        if (entity.DanceStyle != AvatarActions.NoDance)
            Broadcast(new DanceMessageComposer(entity.RoomIndex, entity.DanceStyle));
        SendTo(playerId, new BotRemovedFromInventoryComposer(botId));
    }

    public async Task RemoveBotAsync(Guid playerId, int botId)
    {
        if (_entities?.Find(playerId) is not { } picker || _entities.FindBot(botId) is not { Bot: { } bot } entity
            || bot.Has(BotSkills.NoPickUp)
            || (bot.Data.OwnerId != playerId && picker.ControllerLevel < RoomControllerLevels.Rights))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Bots.FirstOrDefaultAsync(b => b.Id == botId) is not { } row)
            return;

        row.RoomId = null;
        row.Rotation = entity.BodyDirection;
        await db.SaveChangesAsync();

        _entities.RemoveBot(botId);
        Broadcast(new UserRemoveMessageComposer(entity.RoomIndex));

        // it goes back to whoever owns it, wherever they are
        var returned = new BotAddedToInventoryComposer(BotData.From(row), OpenInventory: false);
        await GrainFactory.GetGrain<IPlayerGrain>(row.OwnerId).SendAsync(returned.ToMessage());
    }

    public Task SendBotConfigurationAsync(Guid playerId, int botId, int skillId)
    {
        if (_entities?.FindBot(botId) is not { Bot: { } bot } || bot.Data.OwnerId != playerId || !bot.Has(skillId))
            return Task.CompletedTask;

        var data = skillId switch
        {
            BotSkills.Chatter => string.Join(ChatterSeparator,
                string.Join(ChatterLineSeparator, bot.Data.ChatLines), bot.Data.AutoChat, bot.Data.ChatDelaySeconds, bot.Data.MixSentences),
            BotSkills.ChangeName => bot.Data.Name,
            _ => string.Empty,
        };
        SendTo(playerId, new BotCommandConfigurationComposer(botId, skillId, data));
        return Task.CompletedTask;
    }

    public async Task CommandBotAsync(Guid playerId, int botId, int skillId, string data)
    {
        if (_entities?.Find(playerId) is not { } commander || _entities.FindBot(botId) is not { Bot: { } bot } entity
            || !bot.Has(skillId) || (OwnerSkills.Contains(skillId) && bot.Data.OwnerId != playerId))
            return;

        // one muted by staff puts no words in their bot's mouth either
        if (skillId is BotSkills.Chatter or BotSkills.ChangeName
            && commander.Player.MutedUntil is { } muted && muted > DateTimeOffset.UtcNow)
            return;

        switch (skillId)
        {
            case BotSkills.Figure:
                await SaveBotAsync(bot, row =>
                {
                    row.Figure = commander.Player.Figure;
                    row.Gender = commander.Player.Gender;
                });
                entity.Player = bot.Avatar();
                Broadcast(new UserChangeMessageComposer(entity.RoomIndex, bot.Data.Figure, bot.Data.Gender.ToLetter(), bot.Data.Motto, AchievementScore: 0));
                break;

            case BotSkills.Chatter:
                await ConfigureChatterAsync(bot, data);
                break;

            case BotSkills.RandomWalk:
                await SaveBotAsync(bot, row => row.WalksRandomly = !row.WalksRandomly);
                bot.NextWalkTick = _entities.CurrentTick;
                break;

            case BotSkills.Dance:
                var style = bot.Data.DanceStyle == AvatarActions.NoDance ? _botRandom.Next(AvatarActions.FreeDance, AvatarActions.LastDance + 1) : AvatarActions.NoDance;
                await SaveBotAsync(bot, row => row.DanceStyle = style);
                if (_entities.Dance(entity, style))
                    Broadcast(new DanceMessageComposer(entity.RoomIndex, style));
                break;

            case BotSkills.ChangeName:
                await RenameBotAsync(playerId, bot, entity, data);
                break;
        }
    }

    private async Task ConfigureChatterAsync(RoomBot bot, string data)
    {
        var rules = botOptions.Value;
        var parts = data.Split(ChatterSeparator);
        if (parts.Length < 3)
            return;

        var lines = new List<string>();
        foreach (var line in parts[0].Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(rules.MaxChatLines))
        {
            var said = line.Length > rules.MaxChatLineLength ? line[..rules.MaxChatLineLength] : line;
            lines.Add((await wordFilter.FilterAsync(said, _filterWords)).Text);
        }

        var autoChat = IsTrue(parts[1]);
        var delay = int.TryParse(parts[2], out var seconds) ? Math.Clamp(seconds, rules.MinChatDelaySeconds, rules.MaxChatDelaySeconds) : rules.MinChatDelaySeconds;
        var mix = parts.Length > 3 && IsTrue(parts[3]);
        await SaveBotAsync(bot, row =>
        {
            row.ChatLines = lines;
            row.AutoChat = autoChat;
            row.ChatDelaySeconds = delay;
            row.MixSentences = mix;
        });
        bot.NextChatTick = _entities!.CurrentTick + ChatDelayTicks(bot);
    }

    /// <summary>The client writes a setting as "true" or "1".</summary>
    private static bool IsTrue(string value) => bool.TryParse(value, out var flag) ? flag : value == "1";

    private async Task RenameBotAsync(Guid playerId, RoomBot bot, RoomEntityStore.RoomEntity entity, string name)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > Habbo.Domain.Bots.Bot.NameMaxLength
            || (await wordFilter.FilterAsync(name, _filterWords)).FirstHit is not null)
        {
            SendTo(playerId, new BotErrorComposer(BotErrorComposer.NameNotAccepted));
            return;
        }

        await SaveBotAsync(bot, row => row.Name = name);
        entity.Player = bot.Avatar();
        Broadcast(new UserNameChangedMessageComposer(bot.Data.Id, entity.RoomIndex, name));
    }

    /// <summary>Changes the bot's row, and the room's copy of it the same way.</summary>
    private async Task SaveBotAsync(RoomBot bot, Action<Habbo.Domain.Bots.Bot> change)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Bots.FirstOrDefaultAsync(b => b.Id == bot.Data.Id) is not { } row)
            return;

        change(row);
        change(bot.Data);
        await db.SaveChangesAsync();
    }

    private static int ChatDelayTicks(RoomBot bot) => Math.Max(1, bot.Data.ChatDelaySeconds) * TicksPerSecond;

    /// <summary>What bots do by themselves each tick: say something, and set off somewhere.</summary>
    private async Task BotsActAsync()
    {
        if (_entities is null)
            return;

        var tick = _entities.CurrentTick;
        foreach (var entity in _entities.Bots.ToList())
        {
            var bot = entity.Bot!;
            if (bot.Has(BotSkills.Chatter) && bot.Data.AutoChat && bot.Data.ChatLines.Count > 0 && tick >= bot.NextChatTick)
            {
                bot.NextChatTick = tick + ChatDelayTicks(bot);
                var line = bot.Data.MixSentences
                    ? SentenceMixer.Mix(bot.Data.ChatLines, _botRandom)
                    : bot.Data.ChatLines[_botRandom.Next(bot.Data.ChatLines.Count)];
                await BotSaysAsync(entity, line);
            }

            // following someone for wired comes before walking about
            if (bot.Following is not null)
            {
                Follow(entity, bot);
                continue;
            }

            if (bot.Has(BotSkills.RandomWalk) && bot.Data.WalksRandomly && !entity.Walking && tick >= bot.NextWalkTick)
            {
                bot.NextWalkTick = tick + botOptions.Value.RandomWalkPauseSeconds * TicksPerSecond;
                if (_entities.RandomReachableTile(entity, _botRandom) is { } tile)
                    _entities.Walk(entity, tile.X, tile.Y);
            }
        }
    }

    /// <summary>A bot following someone sets off after them each time they have moved away from it, and stops
    /// following once they have left.</summary>
    private void Follow(RoomEntityStore.RoomEntity entity, RoomBot bot)
    {
        if (bot.Following is not { } followed || _entities!.Find(followed.Player.Id) != followed)
        {
            bot.Following = null;
            bot.FollowedAt = null;
            return;
        }

        if (bot.FollowedAt == (followed.X, followed.Y) || (Math.Abs(followed.X - entity.X) <= 1 && Math.Abs(followed.Y - entity.Y) <= 1))
            return;

        bot.FollowedAt = (followed.X, followed.Y);
        var (x, y) = NearestFreeNeighbour(followed, entity);
        _entities.Walk(entity, x, y);
    }

    // ---------- what wired's bot boxes have bots do ----------

    private async Task WiredBotSayAsync(RoomEntityStore.RoomEntity bot, string text, RoomChatKind kind, int bubbleWidth,
        IReadOnlyList<RoomEntityStore.RoomEntity>? listeners)
    {
        if (_entities is null)
            return;

        var message = new ChatMessage(bot.RoomIndex, text, await gestures.DetectAsync(text), _botChatStyle, Links: [], TrackingId: 0, bubbleWidth);
        if (listeners is null)
            SendChat(message, kind, _entities.Entities, toSpectators: true);
        else
            SendChat(message, kind, listeners, toSpectators: false);
    }

    /// <summary>A look as the client writes one: parts of a set type, a set and its colours, joined by dots.</summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z]{2}-\d+(-\d+)*(\.[a-z]{2}-\d+(-\d+)*)*$")]
    private static partial System.Text.RegularExpressions.Regex FigureSyntax();

    /// <summary>The bot puts on the look and keeps it. Only a look written as the client writes one: the box's text is
    /// the client's copy of its owner's look, but a crafted save could put anything there.</summary>
    private async Task DressBotAsync(RoomEntityStore.RoomEntity entity, string figure)
    {
        if (entity.Bot is not { } bot || !FigureSyntax().IsMatch(figure) || bot.Data.Figure == figure)
            return;

        await SaveBotAsync(bot, row => row.Figure = figure);
        entity.Player = bot.Avatar();
        Broadcast(new UserChangeMessageComposer(entity.RoomIndex, bot.Data.Figure, bot.Data.Gender.ToLetter(), bot.Data.Motto, AchievementScore: 0));
    }

    /// <summary>The receiver holds the hand item; a giver turns to them and hands it over, as a user hands one on.</summary>
    private Task HandOverAsync(RoomEntityStore.RoomEntity? giver, RoomEntityStore.RoomEntity receiver, int handItem)
    {
        if (giver is not null)
            LookAt(giver, receiver.X, receiver.Y);
        SetHandItem(receiver, handItem);
        if (giver is not null && receiver.Bot is null && receiver.Pet is null)
            SendTo(receiver.Player.Id, new HandItemReceivedMessageComposer(giver.RoomIndex, handItem));
        return Task.CompletedTask;
    }

    /// <summary>The walker goes up next to the target, then does something if they are still next to each other.</summary>
    private Task WalkUpToAsync(RoomEntityStore.RoomEntity walker, RoomEntityStore.RoomEntity target, Func<Task> then)
    {
        var (x, y) = NearestFreeNeighbour(target, walker);
        return WalkThenAsync(walker, x, y, () =>
            Math.Abs(walker.X - target.X) <= 1 && Math.Abs(walker.Y - target.Y) <= 1 ? then() : Task.CompletedTask);
    }

    private async Task BotSaysAsync(RoomEntityStore.RoomEntity bot, string text, Guid? onlyTo = null)
    {
        var message = new ChatMessage(bot.RoomIndex, text, await gestures.DetectAsync(text), _botChatStyle, Links: [], TrackingId: 0);
        if (onlyTo is { } playerId)
        {
            if (_entities?.Find(playerId) is { } listener)
                SendChat(message, RoomChatKind.Whisper, [listener], toSpectators: false);
        }
        else if (_entities is not null)
            SendChat(message, RoomChatKind.Talk, _entities.Entities, toSpectators: true);
    }

    /// <summary>A bot that serves hears what was said near it: a drink it serves, asked for by name, it
    /// brings over.</summary>
    private async Task BartendersHearAsync(RoomEntityStore.RoomEntity speaker, string text)
    {
        if (_entities is null)
            return;

        var reach = botOptions.Value.ServeReach;
        var bartender = _entities.Bots
            .Where(b => b.Bot!.Has(BotSkills.ServeBeverage) && b.Bot.Type.Beverages.Count > 0
                        && Math.Max(Math.Abs(b.X - speaker.X), Math.Abs(b.Y - speaker.Y)) <= reach)
            .OrderBy(b => Math.Max(Math.Abs(b.X - speaker.X), Math.Abs(b.Y - speaker.Y)))
            .FirstOrDefault();
        if (bartender is null)
            return;

        // the longest name heard wins, so "tap water" is not taken for "water"
        (int Item, string Name)? order = null;
        foreach (var item in bartender.Bot!.Type.Beverages)
        {
            if (await gameTexts.GetAsync(HandItemTextPrefix + item) is { Length: > 0 } name
                && text.Contains(name, StringComparison.OrdinalIgnoreCase) && name.Length > (order?.Name.Length ?? 0))
                order = (item, name);
        }

        if (order is not { } served)
            return;

        var (handItem, drink) = served;
        var (x, y) = NearestFreeNeighbour(speaker, bartender);
        await WalkThenAsync(bartender, x, y, async () =>
        {
            if (_entities.Find(speaker.Player.Id) is null)
                return;

            LookAt(bartender, speaker.X, speaker.Y);
            SetHandItem(speaker, handItem);
            SendTo(speaker.Player.Id, new HandItemReceivedMessageComposer(bartender.RoomIndex, handItem));
            await BotSaysAsync(bartender, botOptions.Value.ServedText.Replace(UsernameToken, speaker.Player.Username).Replace(ItemToken, drink));
        });
    }

    /// <summary>When the player last came into the room before now, for a visitor logger to count from;
    /// null when they never have. Only asked when the room has one.</summary>
    private async Task<DateTimeOffset?> PreviousVisitAsync(Guid playerId)
    {
        if (_room is null || _entities?.Bots.Any(b => b.Bot!.Type.LogsVisitors) != true)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.RoomVisits.Where(v => v.RoomId == _room.Id && v.UserId == playerId)
            .OrderByDescending(v => v.Id).Select(v => (DateTimeOffset?)v.EnteredAt).FirstOrDefaultAsync();
    }

    /// <summary>A visitor logger greets whoever comes in, and tells its owner coming back who came by.</summary>
    private async Task VisitorLoggersGreetAsync(RoomEntityStore.RoomEntity visitor, DateTimeOffset? previousVisit)
    {
        if (_room is null || _entities is null)
            return;

        var rules = botOptions.Value;
        foreach (var visitorLogger in _entities.Bots.Where(b => b.Bot!.Type.LogsVisitors).ToList())
        {
            var bot = visitorLogger.Bot!;
            if (bot.Data.OwnerId != visitor.Player.Id)
            {
                await BotSaysAsync(visitorLogger, rules.VisitorGreetingText.Replace(UsernameToken, visitor.Player.Username).Replace(OwnerToken, bot.OwnerName),
                    visitor.Player.Id);
                continue;
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var since = previousVisit ?? DateTimeOffset.MinValue;
            var visitors = await db.RoomVisits.Where(v => v.RoomId == _room.Id && v.UserId != visitor.Player.Id && v.EnteredAt > since)
                .Join(db.Users, v => v.UserId, u => u.Id, (v, u) => u.Username).Distinct().OrderBy(name => name).ToListAsync();
            await BotSaysAsync(visitorLogger, visitors.Count == 0
                ? rules.NoVisitorsText
                : rules.VisitorsReportText.Replace(VisitorsToken, string.Join(", ", visitors)), visitor.Player.Id);
        }
    }
}
