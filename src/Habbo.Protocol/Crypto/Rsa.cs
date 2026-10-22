using System.Numerics;

namespace Habbo.Protocol.Crypto;

/// <summary>An RSA private key as used by the Habbo handshake. The client embeds the matching
/// public modulus/exponent (e = 3) and verifies our signatures.</summary>
public sealed record RsaPrivateKey(BigInteger Modulus, BigInteger Exponent, BigInteger PrivateExponent);

public static class Rsa
{
    /// <summary>PKCS#1 v1.5 type 1 signature — no hashing, Habbo signs the raw decimal prime/generator
    /// strings. Block layout: 00 01 FF..FF 00 || data, then m^d mod n as a fixed-length octet string.</summary>
    public static byte[] SignType1(ReadOnlySpan<byte> data, RsaPrivateKey key)
    {
        var blockLength = (int)((key.Modulus.GetBitLength() + 7) / 8);
        if (data.Length > blockLength - 11)
            throw new ArgumentException($"Data too long for RSA block: {data.Length} > {blockLength - 11}.");

        var block = new byte[blockLength];
        block[1] = 0x01;
        new Span<byte>(block, 2, blockLength - 3 - data.Length).Fill(0xFF);
        data.CopyTo(new Span<byte>(block, blockLength - data.Length, data.Length));

        var message = new BigInteger(block, isUnsigned: true, isBigEndian: true);
        var signature = BigInteger.ModPow(message, key.PrivateExponent, key.Modulus);

        var signatureBytes = signature.ToByteArray(isUnsigned: true, isBigEndian: true);
        var padded = new byte[blockLength];
        signatureBytes.CopyTo(new Span<byte>(padded, blockLength - signatureBytes.Length, signatureBytes.Length));
        return padded;
    }

    /// <summary>PKCS#1 v1.5 type 2 decryption — the client's encrypted DH public key arrives in this form.</summary>
    public static byte[] DecryptType2(ReadOnlySpan<byte> ciphertext, RsaPrivateKey key)
    {
        var blockLength = (int)((key.Modulus.GetBitLength() + 7) / 8);

        var cipher = new BigInteger(ciphertext, isUnsigned: true, isBigEndian: true);
        var message = BigInteger.ModPow(cipher, key.PrivateExponent, key.Modulus);

        var messageBytes = message.ToByteArray(isUnsigned: true, isBigEndian: true);
        var block = new byte[blockLength];
        messageBytes.CopyTo(new Span<byte>(block, blockLength - messageBytes.Length, messageBytes.Length));

        if (block[0] != 0x00 || block[1] != 0x02)
            throw new InvalidDataException("Invalid PKCS#1 v1.5 type 2 block.");

        var separator = Array.IndexOf(block, (byte)0x00, 2);
        if (separator < 10)
            throw new InvalidDataException("Invalid PKCS#1 v1.5 type 2 padding.");

        return block[(separator + 1)..];
    }
}
