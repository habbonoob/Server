using System.Numerics;
using Habbo.Protocol.Crypto;

namespace Habbo.Protocol.Tests;

/// <summary>Verifies the real HabboAir key material: the private exponent must invert the
/// public key embedded in the WIN63 client. If this fails, the handshake can never succeed.</summary>
public class ClientKeyConsistencyTests
{
    /// <summary>The server's key, from src/Habbo.Host/appsettings.Local.json, which git ignores:
    /// the real key never enters the repository.</summary>
    private static readonly Lazy<RsaPrivateKey> LocalKey = new(LoadLocalKey);

    private static RsaPrivateKey RealKey => LocalKey.Value;

    private static RsaPrivateKey LoadLocalKey()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
            directory = directory.Parent;

        var path = directory is null ? null : Path.Combine(directory.FullName, "src", "Habbo.Host", "appsettings.Local.json");
        if (path is null || !File.Exists(path))
            throw new InvalidOperationException(
                "No RSA key to check: put Crypto:Rsa:Modulus and Crypto:Rsa:PrivateExponent in src/Habbo.Host/appsettings.Local.json.");

        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var rsa = json.RootElement.GetProperty("Crypto").GetProperty("Rsa");
        BigInteger Hex(string name) =>
            BigInteger.Parse("0" + rsa.GetProperty(name).GetString(), System.Globalization.NumberStyles.HexNumber);
        return new RsaPrivateKey(Hex("Modulus"), new BigInteger(3), Hex("PrivateExponent"));
    }

    [Fact]
    public void SignatureInvertsWithTheClientsEmbeddedPublicKey()
    {
        // The exact data the client will verify: the decimal DH prime string.
        var data = System.Text.Encoding.Latin1.GetBytes("114670925920269957593299136150366949983142588366300079186349531");

        var signature = Rsa.SignType1(data, RealKey);
        Assert.Equal(128, signature.Length);

        // Client-side verify: s^e mod n must be the PKCS#1 v1.5 type 1 padded block.
        var signatureValue = new BigInteger(signature, isUnsigned: true, isBigEndian: true);
        var recovered = BigInteger.ModPow(signatureValue, RealKey.Exponent, RealKey.Modulus);
        var block = recovered.ToByteArray(isUnsigned: true, isBigEndian: true);
        var padded = new byte[128];
        block.CopyTo(padded.AsSpan(128 - block.Length));

        Assert.Equal(0x00, padded[0]);
        Assert.Equal(0x01, padded[1]);
        Assert.Equal(0x00, padded[128 - data.Length - 1]);
        Assert.Equal(data, padded[(128 - data.Length)..]);
    }

    [Fact]
    public void Type2DecryptionWorksWithTheRealKey()
    {
        // Round-trip: encrypt with the client's public key, decrypt with the real private key.
        var data = System.Text.Encoding.Latin1.GetBytes("1234567890");
        var block = new byte[128];
        block[1] = 0x02;
        block.AsSpan(2, 128 - 3 - data.Length).Fill(0xAB); // non-zero padding string (≥ 8 bytes)
        block[128 - data.Length - 1] = 0x00;
        data.CopyTo(block.AsSpan(128 - data.Length));

        var message = new BigInteger(block, isUnsigned: true, isBigEndian: true);
        var cipher = BigInteger.ModPow(message, RealKey.Exponent, RealKey.Modulus);
        var cipherBytes = cipher.ToByteArray(isUnsigned: true, isBigEndian: true);
        var paddedCipher = new byte[128];
        cipherBytes.CopyTo(paddedCipher.AsSpan(128 - cipherBytes.Length));

        Assert.Equal(data, Rsa.DecryptType2(paddedCipher, RealKey));
    }
}
