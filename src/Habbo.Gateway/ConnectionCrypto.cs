using System.Numerics;
using System.Text;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.Frames;
using Habbo.Protocol.IO;

namespace Habbo.Gateway;

/// <summary>Per-connection handshake state: RSA-signed DH exchange, then RC4 both directions.
/// Pure state — all sending goes through the session's outbound channel, where
/// <see cref="Complete"/> decides whether the frame is encrypted.</summary>
/// <param name="primeBits">The size of the connection's random DH prime (<see cref="CryptoOptions.DiffieHellmanBits"/>).</param>
public sealed class ConnectionCrypto(RsaPrivateKey rsaKey, int primeBits)
{
    private BigInteger _prime;
    private BigInteger _generator;
    private BigInteger _serverPrivateKey;
    private BigInteger _serverPublicKey;

    private enum Stage { None, Begun, Complete }

    private Stage _stage;

    public FrameDecoder Decoder { get; } = new();
    public Rc4? OutputCipher { get; private set; }
    public bool Complete => _stage == Stage.Complete;

    /// <summary>RSA type-1 signed prime/generator, hex-encoded, ready for 1142.</summary>
    public string SignedPrime { get; private set; } = string.Empty;
    public string SignedGenerator { get; private set; } = string.Empty;

    /// <summary>RSA type-1 signed server DH public key, hex-encoded, ready for 1038.</summary>
    public string SignedServerPublicKey { get; private set; } = string.Empty;

    /// <summary>Client asked for DH parameters (2735): pick this connection's random prime and generator and our
    /// keypair, and sign the prime and generator. False when it asked already: a handshake is begun once, so a client
    /// cannot make the server generate and sign over and over on one connection.</summary>
    public bool BeginHandshake()
    {
        if (_stage != Stage.None)
            return false;

        (_prime, _generator) = DiffieHellman.GenerateParameters(primeBits);
        _serverPrivateKey = DiffieHellman.GeneratePrivateKey();
        _serverPublicKey = DiffieHellman.ComputePublicKey(_prime, _generator, _serverPrivateKey);

        SignedPrime = SignDecimal(_prime, rsaKey);
        SignedGenerator = SignDecimal(_generator, rsaKey);
        _stage = Stage.Begun;
        return true;
    }

    /// <summary>Client sent its RSA-encrypted DH public key (764): derive the shared secret and switch RC4 on.
    /// False when the handshake was not begun, is complete already, or the key is not one DH allows.</summary>
    public bool ProcessClientPublicKey(MessageReader reader)
    {
        if (_stage != Stage.Begun)
            return false;

        var encryptedClientKey = Convert.FromHexString(reader.ReadString());
        var decryptedClientKey = Rsa.DecryptType2(encryptedClientKey, rsaKey);
        var clientPublicKey = BigInteger.Parse(Encoding.Latin1.GetString(decryptedClientKey));
        if (!DiffieHellman.IsValidPublicKey(_prime, clientPublicKey))
            return false;

        var sharedSecret = DiffieHellman.ComputeSharedSecret(_prime, clientPublicKey, _serverPrivateKey);
        var rc4Key = DiffieHellman.DeriveRc4Key(sharedSecret);

        Decoder.Cipher = new Rc4(rc4Key);
        OutputCipher = new Rc4(rc4Key);
        SignedServerPublicKey = SignDecimal(_serverPublicKey, rsaKey);
        _stage = Stage.Complete;
        return true;
    }

    private static string SignDecimal(BigInteger value, RsaPrivateKey key)
    {
        var bytes = Encoding.Latin1.GetBytes(value.ToString());
        return Convert.ToHexString(Rsa.SignType1(bytes, key));
    }
}
