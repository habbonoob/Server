using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Habbo.Gateway.Messages.Handlers.Handshake;
using Habbo.Protocol;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Handshake;
using Microsoft.Extensions.Logging.Abstractions;

namespace Habbo.Gateway.Tests;

/// <summary>UniqueID comes as [machineId, fingerprint, version], or without the machine id when the client has none:
/// EvaWireFormat writes nothing for a null. habbo.com, 4 November 2026: a client with no machine id sent the fingerprint
/// and the version only, and was given one (UniqueMachineID) before AuthenticationOK.</summary>
public class UniqueIdHandlerTests
{
    private static MessageReader Payload(params string[] strings)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessageWriter(buffer);
        foreach (var s in strings)
            writer.WriteString(s);
        return new MessageReader(buffer.WrittenSpan.ToArray());
    }

    [Fact]
    public void TheMachineIdIsTheFirstOfThreeStringsAndMissingFromTwo()
    {
        var full = UniqueIDMessageEvent.Read(Payload("~a1b2", "~fingerprint", "WIN/51,3,4,0"));
        Assert.Equal("~a1b2", full.MachineId);
        Assert.Equal("~fingerprint", full.Fingerprint);

        var none = UniqueIDMessageEvent.Read(Payload("~fingerprint", "WIN/51,3,4,0"));
        Assert.Equal(string.Empty, none.MachineId);
        Assert.Equal("~fingerprint", none.Fingerprint);
    }

    [Fact]
    public async Task AClientWithoutAMachineIdIsGivenOne()
    {
        var (context, session) = Context();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var handler = new UniqueIdHandler(NullLogger<UniqueIdHandler>.Instance);

        await handler.HandleAsync(context, UniqueIDMessageEvent.Read(Payload("~fingerprint", "WIN/51,3,4,0")), cts.Token);

        var frame = await session.Outbound.Reader.ReadAsync(cts.Token);
        Assert.Equal(ServerMessages.UniqueMachineIDComposer, frame.Message.Header);
        Assert.Matches("^[0-9a-f]{32}$", session.MachineId);
    }

    [Fact]
    public async Task AClientWithAMachineIdKeepsIt()
    {
        var (context, session) = Context();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var handler = new UniqueIdHandler(NullLogger<UniqueIdHandler>.Instance);

        await handler.HandleAsync(context, UniqueIDMessageEvent.Read(Payload("~a1b2", "~fingerprint", "WIN/51,3,4,0")), cts.Token);

        Assert.Equal("~a1b2", session.MachineId);
        Assert.False(session.Outbound.Reader.TryRead(out _));
    }

    private static (GatewaySessionContext Context, Session Session) Context()
    {
        var session = new Session { Id = "test", RemoteEndPoint = new IPEndPoint(IPAddress.None, 0) };
        using var rsa = System.Security.Cryptography.RSA.Create(1024);
        var key = rsa.ExportParameters(includePrivateParameters: true);
        static System.Numerics.BigInteger Number(byte[] bytes) => new(bytes, isUnsigned: true, isBigEndian: true);
        var crypto = new ConnectionCrypto(new RsaPrivateKey(Number(key.Modulus!), Number(key.Exponent!), Number(key.D!)), 256);
        return (new GatewaySessionContext { Session = session, Crypto = crypto, Output = new Pipe().Writer }, session);
    }
}
