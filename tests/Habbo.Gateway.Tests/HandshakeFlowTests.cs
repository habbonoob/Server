using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.IO.Pipelines;
using System.Net;
using System.Numerics;
using Habbo.Protocol;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Gateway.Tests;

public class HandshakeFlowTests
{
    /// <summary>A throwaway key the client's size: the handshake works the same with any key, so
    /// the server's real one stays out of the repository.</summary>
    private static readonly RsaPrivateKey TestKey = CreateTestKey();

    private static RsaPrivateKey CreateTestKey()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(1024);
        var key = rsa.ExportParameters(includePrivateParameters: true);
        static BigInteger Number(byte[] bytes) => new(bytes, isUnsigned: true, isBigEndian: true);
        return new RsaPrivateKey(Number(key.Modulus!), Number(key.Exponent!), Number(key.D!));
    }

    /// <summary>The prime size the server runs with (CryptoOptions.DiffieHellmanBits).</summary>
    private const int PrimeBits = 256;

    [Fact]
    public void AHandshakeIsBegunOnceAndCompletedOnlyAfterItIsBegun()
    {
        var crypto = new ConnectionCrypto(TestKey, PrimeBits);
        var payload = new ArrayBufferWriter<byte>();
        new MessageWriter(payload).WriteString("00");

        Assert.False(crypto.ProcessClientPublicKey(new MessageReader(payload.WrittenMemory)));
        Assert.True(crypto.BeginHandshake());
        Assert.False(crypto.BeginHandshake());
        // every connection gets its own random prime and generator
        var other = new ConnectionCrypto(TestKey, PrimeBits);
        Assert.True(other.BeginHandshake());
        Assert.NotEqual(crypto.SignedPrime, other.SignedPrime);
        Assert.NotEqual(crypto.SignedGenerator, other.SignedGenerator);
    }

    [Fact]
    public async Task HandshakeFramesGoOutPlaintext()
    {
        var crypto = new ConnectionCrypto(TestKey, PrimeBits);
        var session = new Session { Id = "test", RemoteEndPoint = new IPEndPoint(IPAddress.None, 0) };
        var context = new GatewaySessionContext { Session = session, Crypto = crypto, Output = new Pipe().Writer };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // 1142 — signed prime/generator, plaintext.
        crypto.BeginHandshake();
        Assert.Equal(256, crypto.SignedPrime.Length);
        await context.SendPlaintextAsync(
            new InitDiffieHandshakeComposer(crypto.SignedPrime, crypto.SignedGenerator), cts.Token);

        var initDiffie = await session.Outbound.Reader.ReadAsync(cts.Token);
        Assert.False(initDiffie.Encrypted);
        Assert.Equal(ServerMessages.InitDiffieHandshakeComposer, initDiffie.Message.Header);

        var encoded = new ArrayBufferWriter<byte>();
        SessionWriter.Encode(encoded, initDiffie, crypto.OutputCipher);
        Assert.Equal(518, BinaryPrimitives.ReadInt32BigEndian(encoded.WrittenSpan));

        // The client's 764 payload: RSA type-2 encrypted decimal DH public key, hex string.
        var clientKey = BuildType2Ciphertext("1234567890", TestKey);
        var payload = new ArrayBufferWriter<byte>();
        WriteHexString(payload, clientKey);
        crypto.ProcessClientPublicKey(new MessageReader(payload.WrittenMemory));
        Assert.True(crypto.Complete);

        // 1038 — REGRESSION: the client installs its ciphers only AFTER parsing this frame.
        // Even though Complete is now true, this frame must go out unencrypted.
        await context.SendPlaintextAsync(
            new CompleteDiffieHandshakeComposer(crypto.SignedServerPublicKey, EnableEncryption: true), cts.Token);

        var completeDiffie = await session.Outbound.Reader.ReadAsync(cts.Token);
        Assert.False(completeDiffie.Encrypted);

        encoded = new ArrayBufferWriter<byte>();
        SessionWriter.Encode(encoded, completeDiffie, crypto.OutputCipher);
        Assert.Equal(261, BinaryPrimitives.ReadInt32BigEndian(encoded.WrittenSpan)); // 2 header + 258 string + 1 bool
        Assert.Equal(ServerMessages.CompleteDiffieHandshakeComposer, BinaryPrimitives.ReadUInt16BigEndian(encoded.WrittenSpan[4..6]));
        Assert.Equal(0x01, encoded.WrittenSpan[^1]); // trailing true: enable server→client RC4
    }

    [Fact]
    public async Task PostHandshakeFramesAreEncrypted()
    {
        var crypto = new ConnectionCrypto(TestKey, PrimeBits);
        crypto.BeginHandshake();
        var clientKey = BuildType2Ciphertext("1234567890", TestKey);
        var payload = new ArrayBufferWriter<byte>();
        WriteHexString(payload, clientKey);
        crypto.ProcessClientPublicKey(new MessageReader(payload.WrittenMemory));

        var session = new Session { Id = "test", RemoteEndPoint = new IPEndPoint(IPAddress.None, 0) };
        var context = new GatewaySessionContext { Session = session, Crypto = crypto, Output = new Pipe().Writer };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await context.SendAsync(
            new AuthenticationOKMessageComposer(AccountId: 1, [], IdentityId: 0), cts.Token);

        var frame = await session.Outbound.Reader.ReadAsync(cts.Token);
        Assert.True(frame.Encrypted);

        var encoded = new ArrayBufferWriter<byte>();
        SessionWriter.Encode(encoded, frame, crypto.OutputCipher);
        // An encrypted length prefix is garbage — this must not read as the plaintext length (6).
        Assert.NotEqual(6, BinaryPrimitives.ReadInt32BigEndian(encoded.WrittenSpan));
    }

    // Keeps the ref struct MessageWriter out of the async method body.
    private static void WriteHexString(IBufferWriter<byte> output, byte[] bytes)
    {
        var writer = new MessageWriter(output);
        writer.WriteString(Convert.ToHexString(bytes));
    }

    private static byte[] BuildType2Ciphertext(string decimalValue, RsaPrivateKey key)
    {
        var data = System.Text.Encoding.Latin1.GetBytes(decimalValue);
        var block = new byte[128];
        block[1] = 0x02;
        block.AsSpan(2, 128 - 3 - data.Length).Fill(0xAB);
        block[128 - data.Length - 1] = 0x00;
        data.CopyTo(block.AsSpan(128 - data.Length));

        var message = new BigInteger(block, isUnsigned: true, isBigEndian: true);
        var cipher = BigInteger.ModPow(message, key.Exponent, key.Modulus);
        var cipherBytes = cipher.ToByteArray(isUnsigned: true, isBigEndian: true);
        var padded = new byte[128];
        cipherBytes.CopyTo(padded.AsSpan(128 - cipherBytes.Length));
        return padded;
    }
}
