using System.IO.Pipelines;
using System.Net;
using Habbo.Gateway.Messages.Handlers.Room.Session;
using Habbo.Protocol;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.Messages.Incoming.Room.Session;

namespace Habbo.Gateway.Tests;

public class QuitHandlerTests
{
    /// <summary>A throwaway key: nothing here is encrypted, the context only needs one.</summary>
    private static RsaPrivateKey TestKey()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(1024);
        var key = rsa.ExportParameters(includePrivateParameters: true);
        static System.Numerics.BigInteger Number(byte[] bytes) => new(bytes, isUnsigned: true, isBigEndian: true);
        return new RsaPrivateKey(Number(key.Modulus!), Number(key.Exponent!), Number(key.D!));
    }

    /// <summary>habbo.com, 4 November 2026: Quit is answered with YouArePlayingGame false, then CloseConnection.</summary>
    [Fact]
    public async Task QuitEndsTheGameAndClosesTheRoomConnection()
    {
        var session = new Session { Id = "test", RemoteEndPoint = new IPEndPoint(IPAddress.None, 0) };
        var context = new GatewaySessionContext
        {
            Session = session,
            Crypto = new ConnectionCrypto(TestKey(), 256),
            Output = new Pipe().Writer,
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        Assert.True(await new QuitHandler().HandleAsync(context, new QuitMessageEvent(), cts.Token));

        var first = await session.Outbound.Reader.ReadAsync(cts.Token);
        var second = await session.Outbound.Reader.ReadAsync(cts.Token);
        Assert.Equal(ServerMessages.YouArePlayingGameMessageComposer, first.Message.Header);
        Assert.Equal(ServerMessages.CloseConnectionMessageComposer, second.Message.Header);
        Assert.False(session.Outbound.Reader.TryRead(out _));
    }
}
