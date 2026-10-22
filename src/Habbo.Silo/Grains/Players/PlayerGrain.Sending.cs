using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Players;

/// <summary>What the account's features share: answering the client and finding the account.</summary>
public sealed partial class PlayerGrain
{
    private Guid PlayerId => this.GetPrimaryKey();

    /// <summary>Sends to every session the player has open.</summary>
    private void Send(IComposer composer)
    {
        var message = composer.ToMessage();
        foreach (var session in _sessions)
            sink.TrySend(session, message);
    }

    /// <summary>Finds the account. A membership that ran out takes the club level with it here, so every
    /// load (login included) sees the level the player still has.</summary>
    private async Task<User> LoadUserAsync(AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == PlayerId)
                   ?? throw new InvalidOperationException($"Player {PlayerId} does not exist.");
        if (user.ClubLevel > ClubLevel.None && !await HasActiveMembershipAsync(db))
        {
            user.ClubLevel = ClubLevel.None;
            await db.SaveChangesAsync();
        }
        return user;
    }

    /// <summary>Refreshes the cached view after the account row changed.</summary>
    private PlayerData Remember(User user) => Data = ToPlayerData(user);

    public async Task<PlayerData> ReloadAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return Remember(await LoadUserAsync(db));
    }

    public Task DisconnectAsync(int reason)
    {
        foreach (var session in _sessions)
            DisconnectSession(session, reason);
        return Task.CompletedTask;
    }

    /// <summary>Tells one connection why it is closed, then closes it once that has gone out.</summary>
    private void DisconnectSession(Guid session, int reason)
    {
        var why = new Habbo.Protocol.Messages.Outgoing.Handshake.DisconnectReasonComposer(reason);
        sink.TrySend(session, why.ToMessage());
        sink.Disconnect(session);
    }

    public Task<int?> GetCurrentRoomAsync() => Task.FromResult(_sessions.Count > 0 ? _roomId : null);

    public Task SetCurrentRoomAsync(int? roomId)
    {
        _roomId = roomId;
        // friends' lists show whether the player is in a room, to follow them there
        _roomAnnouncement ??= this.RegisterGrainTimer(AnnounceRoomAsync,
            new GrainTimerCreationOptions { DueTime = RoomAnnouncementDelay, Period = Timeout.InfiniteTimeSpan });
        return Task.CompletedTask;
    }

    /// <summary>Tells the friends the player is in a room now, or no longer, if that changed since they were told.</summary>
    private async Task AnnounceRoomAsync(CancellationToken _)
    {
        // a one-shot timer: disposing it cancels the token it hands its callback, so the announcement doesn't take it
        _roomAnnouncement?.Dispose();
        _roomAnnouncement = null;

        var inRoom = _roomId is not null;
        if (_sessions.Count == 0 || inRoom == _friendsSeeInRoom)
            return;
        _friendsSeeInRoom = inRoom;
        try
        {
            await friendList.AnnouncePresenceAsync(PlayerId, online: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not tell the friends of {PlayerId} they are {State} a room", PlayerId, inRoom ? "in" : "out of");
        }
    }

    /// <summary>Records money moving, for the club's payday and for the record.</summary>
    private void RecordTransaction(AppDbContext db, int currency, int amount, string reason) =>
        db.CurrencyTransactions.Add(new CurrencyTransaction
        {
            UserId = PlayerId,
            Currency = currency,
            Amount = amount,
            Reason = reason[..Math.Min(reason.Length, CurrencyTransaction.ReasonMaxLength)],
        });
}
