using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Silo.Abstractions.Hotel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway;

/// <summary>Pings every connection now and then; the client answers with a pong. One that has gone quiet for too long
/// is told so and closed, so a vanished client does not stay logged in. One that never logs in is closed too, pings
/// or not.</summary>
public sealed class SessionKeepAlive(
    SessionManager sessions, IClientPacketSink sink, IOptions<GameNetworkOptions> options, ILogger<SessionKeepAlive> logger)
    : BackgroundService
{
    /// <summary>How often connections are looked at when they are not pinged: for those that never log in.</summary>
    private static readonly TimeSpan LoginSweepEvery = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rules = options.Value;
        var pings = rules.PingIntervalSeconds > 0;
        if (!pings && rules.LoginTimeoutSeconds <= 0)
            return;

        using var timer = new PeriodicTimer(pings ? TimeSpan.FromSeconds(rules.PingIntervalSeconds) : LoginSweepEvery);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var silentSince = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(rules.PongTimeoutSeconds);
            var loginBy = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(rules.LoginTimeoutSeconds);
            foreach (var session in sessions.All)
            {
                // answering pings keeps a session, not a connection that never logs in: those would hold their place for good
                if (rules.LoginTimeoutSeconds > 0 && session.Player is null && session.ConnectedAt < loginBy)
                {
                    logger.LogInformation("Connection {ConnectionId} did not log in; closing it", session.Id);
                    sink.Disconnect(session.SessionGuid);
                }
                else if (pings && session.LastPongAt < silentSince)
                {
                    logger.LogInformation("Connection {ConnectionId} stopped answering pings; closing it", session.Id);
                    Send(session, new DisconnectReasonComposer(DisconnectReasonComposer.PongTimeout));
                    sink.Disconnect(session.SessionGuid);
                }
                else if (pings)
                    Send(session, new PingMessageComposer());
            }
        }
    }

    private void Send(Session session, IComposer composer) => sink.TrySend(session.SessionGuid, composer.ToMessage());
}
