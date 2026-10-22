using System.Net;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Gateway.Tests;

public class SessionTests
{
    private static Session NewSession() => new() { Id = "test", RemoteEndPoint = new IPEndPoint(IPAddress.None, 0) };

    [Fact]
    public void ASessionThatFallsTooFarBehindIsClosed()
    {
        var session = NewSession();
        var ping = new OutboundFrame(new PingMessageComposer().ToMessage(), Encrypted: false);

        for (var i = 0; i < Session.OutboundLimit; i++)
            Assert.True(session.Enqueue(ping));

        // the one too many closes the queue: nothing more goes in, and the writer ends once it has drained it
        Assert.False(session.Enqueue(ping));
        Assert.False(session.Outbound.Writer.TryWrite(ping));
    }

    [Fact]
    public void AClosedSessionSaysSo()
    {
        var session = NewSession();
        Assert.False(session.Closing);

        session.Close();

        Assert.True(session.Closing);
        Assert.False(session.Outbound.Writer.TryWrite(new OutboundFrame(new PingMessageComposer().ToMessage(), Encrypted: false)));
    }


    [Fact]
    public void SessionsAreFoundByTheirGuidUntilRemoved()
    {
        var sessions = new SessionManager();
        var session = sessions.Create("a", new IPEndPoint(IPAddress.None, 0));

        Assert.Same(session, sessions.FindByGuid(session.SessionGuid));
        sessions.Remove("a");
        Assert.Null(sessions.FindByGuid(session.SessionGuid));
    }
}
