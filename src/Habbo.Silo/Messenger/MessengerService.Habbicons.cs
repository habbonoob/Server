using Habbo.Domain.Friends;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Messenger;

/// <summary>Habbicons in the console (September 2026): sent in place of text, to a friend or a group chat, and kept
/// as a message is. Clients older than that are shown the Habbicon's name.</summary>
public sealed partial class MessengerService
{
    /// <summary>What a text message carries in place of a Habbicon id.</summary>
    private const int NoHabbicon = 0;

    private static ConsoleMessageType TypeOf(ConsoleMessage message) =>
        message.HabbiconId is null ? ConsoleMessageType.Text : ConsoleMessageType.Habbicon;

    public async Task SendHabbiconAsync(Guid playerId, int chatId, int habbiconId, string habbiconName, int confirmationId)
    {
        if (IsFlooding(playerId))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var sender = await db.Users.AsNoTracking().FirstAsync(u => u.Id == playerId);
        var key = Guid.NewGuid().ToString("N");
        int? error = null;
        if (sender.MutedUntil > DateTimeOffset.UtcNow)
            error = InstantMessageErrors.SenderMuted;

        // who reads it, and the conversation it arrives in for them: a group chat's for its members, the sender's for a friend
        var readers = new List<Guid>();
        var readersChatId = chatId;
        var message = new ConsoleMessage { Key = key, SenderId = playerId, Text = habbiconName, HabbiconId = habbiconId };
        if (error is null && chatId < 0)
        {
            var groupId = -chatId;
            var members = await db.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync();
            if (!members.Contains(playerId))
                error = InstantMessageErrors.NotGroupMember;
            else
            {
                message.GroupId = groupId;
                message.Delivered = true;
                readers.AddRange((await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync(members))
                    .Where(id => id != playerId));
            }
        }
        else if (error is null)
        {
            var recipient = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == chatId);
            if (recipient is null || !await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == recipient.Id))
                error = InstantMessageErrors.NotFriend;
            else if (await db.BlockedUsers.AnyAsync(b => b.UserId == recipient.Id && b.BlockedUserId == playerId))
                error = InstantMessageErrors.RecipientUnavailable;
            else if (await db.BlockedUsers.AnyAsync(b => b.UserId == playerId && b.BlockedUserId == recipient.Id))
                error = InstantMessageErrors.SenderUnavailable;
            else
            {
                message.RecipientId = recipient.Id;
                message.Delivered = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([recipient.Id])).Count > 0;
                if (message.Delivered)
                    readers.Add(recipient.Id);
                readersChatId = sender.AccountId;
            }
        }

        if (error is { } code)
        {
            await messenger.SendAsync(playerId, new InstantMessageErrorComposer(code, chatId, NoDetail));
            return;
        }

        db.ConsoleMessages.Add(message);
        await db.SaveChangesAsync();
        await messenger.SendAsync(readers, new NewConsoleMessageComposer(readersChatId, ConsoleMessageType.Habbicon, habbiconName, 0, key,
            NotAConfirmation, sender.AccountId, sender.Username, sender.Figure, habbiconId));
        await messenger.SendAsync(playerId, new NewConsoleMessageComposer(chatId, ConsoleMessageType.Habbicon, habbiconName, 0, key,
            confirmationId, sender.AccountId, sender.Username, sender.Figure, habbiconId));
    }
}
