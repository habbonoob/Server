using System.Globalization;
using System.Numerics;
using Habbo.Protocol.Crypto;

namespace Habbo.Gateway;

public sealed class CryptoOptions
{
    public RsaKeyOptions Rsa { get; set; } = new();

    /// <summary>The size of the random Diffie-Hellman prime each connection gets. 256 bits is 77 decimal digits: past
    /// the 64 the client tries its public key for, within the 117 it can take (see <see cref="DiffieHellman"/>).</summary>
    public int DiffieHellmanBits { get; set; } = 256;
}

public sealed class RsaKeyOptions
{
    /// <summary>Hex-encoded public exponent (e = 3 for the HabboAir-patched client).</summary>
    public string Exponent { get; set; } = "3";

    /// <summary>Hex-encoded modulus — must match the client's embedded public key.</summary>
    public string Modulus { get; set; } = string.Empty;

    /// <summary>Hex-encoded private exponent — the server's signing secret.</summary>
    public string PrivateExponent { get; set; } = string.Empty;

    public RsaPrivateKey ToPrivateKey() => new(
        ParseHex(Modulus),
        ParseHex(Exponent),
        ParseHex(PrivateExponent));

    private static BigInteger ParseHex(string hex)
        => hex.Length == 0 ? BigInteger.Zero : BigInteger.Parse("0" + hex, NumberStyles.HexNumber);
}
