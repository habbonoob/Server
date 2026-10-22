using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Handshake;
using Habbo.Protocol.Messages.Outgoing.Availability;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Moderation;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>2354 SSOTicket — validate the ticket against the silo, bind the player, answer AuthenticationOK.</summary>
[AllowUnauthenticated]
internal sealed class SsoTicketHandler(
    IGrainFactory grains,
    PostAuthenticationSequence postAuthentication,
    IModerationService moderation,
    IHotelAvailabilityService availability,
    FriendPresenceNotifier friendPresence,
    ILogger<SsoTicketHandler> logger)
    : IMessageHandler<SSOTicketMessageEvent>
{
    /// <summary>The client's own error code for a rejected login.</summary>
    private const int LoginFailedErrorCode = -3;

    /// <summary>The longest ticket looked up: the hotel issues 32 hex digits, and each ticket tried is a grain.</summary>
    private const int TicketMaxLength = 128;

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SSOTicketMessageEvent message, CancellationToken ct)
    {
        // a session logs in once: a second ticket would swap it to another account, leaving the first online here
        if (context.Session.Player is not null || message.Ticket.Length > TicketMaxLength)
            return false;

        var authResult = await grains.GetGrain<IAuthGrain>(message.Ticket).AuthenticateAsync();
        if (!authResult.Success || authResult.Player is null)
        {
            logger.LogWarning("Connection {ConnectionId}: SSO failed ({Error})", context.Session.Id, authResult.Error);
            await context.SendAsync(new GenericErrorComposer(LoginFailedErrorCode), ct);
            return false;
        }

        var player = authResult.Player;

        // a banned player, or one on a banned machine, is told why and sent away
        if (await moderation.FindBanAsync(player.Id, context.Session.MachineId) is { } ban)
        {
            logger.LogInformation("Connection {ConnectionId}: {Username} is banned until {Until}", context.Session.Id, player.Username, ban.ExpiresAt);
            await context.SendAsync(new UserBannedMessageComposer(ban.Message), ct);
            return false;
        }

        // while the hotel is closed only those who may stay get in, and everyone else is told when it opens
        if (availability.GetAvailability() is { IsOpen: false } closed && !availability.MayEnterWhileClosed(player))
        {
            logger.LogInformation("Connection {ConnectionId}: {Username} arrived while the hotel is closed", context.Session.Id, player.Username);
            await context.SendAsync(new LoginFailedHotelClosedMessageComposer(closed.ReopenHour, closed.ReopenMinute), ct);
            return false;
        }

        await moderation.RecordMachineAsync(player.Id, context.Session.MachineId);
        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        await playerGrain.SetOnlineAsync(context.Session.SessionGuid);
        context.Session.BindPlayer(player, playerGrain);

        logger.LogInformation("Connection {ConnectionId}: authenticated as {Username} (account {AccountId})",
            context.Session.Id, player.Username, player.AccountId);

        // An empty suggestedLoginActions list sends the client to the hotel view, not onboarding.
        await context.SendAsync(
            new AuthenticationOKMessageComposer(player.AccountId, [], IdentityOf(player)), ct);

        // The client waits for the rest without asking for any of it.
        await postAuthentication.SendAsync(context, player, authResult.PreviousLoginAt, ct);

        // Friends' lists are read once and then only updated, so they are told separately.
        await friendPresence.AnnounceAsync(player.Id, online: true, ct);
        return true;
    }

    /// <summary>Accounts hold exactly one avatar, so the identity is the account itself.</summary>
    private static int IdentityOf(PlayerData player) => player.AccountId;
}
