using System.Numerics;
using System.Security.Cryptography;
using Habbo.Protocol.Crypto;

namespace Habbo.Protocol.Tests;

public class RsaTests
{
    [Fact]
    public void Type1SignatureInvertsToThePaddedBlock()
    {
        var key = GenerateTestKey();
        var data = "114670925920269957593299136150366949983142588366300079186349531"u8.ToArray();

        var signature = Rsa.SignType1(data, key);

        Assert.Equal(128, signature.Length);

        // Verify the structure ourselves: s^e mod n must equal the type-1 padded block.
        var signatureValue = new BigInteger(signature, isUnsigned: true, isBigEndian: true);
        var recovered = BigInteger.ModPow(signatureValue, key.Exponent, key.Modulus);
        var block = recovered.ToByteArray(isUnsigned: true, isBigEndian: true);
        var padded = new byte[128];
        block.CopyTo(padded.AsSpan(128 - block.Length));

        Assert.Equal(0x00, padded[0]);
        Assert.Equal(0x01, padded[1]);
        Assert.Equal(0xFF, padded[2]);
        Assert.Equal(0x00, padded[128 - data.Length - 1]);
        Assert.Equal(data, padded[(128 - data.Length)..]);
    }

    [Fact]
    public void Type2DecryptRoundTrip()
    {
        var key = GenerateTestKey();

        var data = "98765432109876543210"u8.ToArray();
        var ciphertext = EncryptType2(data, key);

        Assert.Equal(data, Rsa.DecryptType2(ciphertext, key));
    }

    [Fact]
    public void Type1RejectsOversizedData()
    {
        var key = GenerateTestKey();
        var data = new byte[128]; // 128 bytes > 117-byte capacity
        Assert.Throws<ArgumentException>(() => Rsa.SignType1(data, key));
    }

    [Fact]
    public void Type2RejectsGarbageCiphertext()
    {
        var key = GenerateTestKey();
        var garbage = new byte[128];
        RandomNumberGenerator.Fill(garbage);
        garbage[0] = 0x00; // keep it block-shaped

        Assert.ThrowsAny<Exception>(() => Rsa.DecryptType2(garbage, key));
    }

    private static byte[] EncryptType2(ReadOnlySpan<byte> data, RsaPrivateKey key)
    {
        var block = new byte[128];
        block[1] = 0x02;
        RandomNumberGenerator.Fill(block.AsSpan(2, 128 - 3 - data.Length));
        block.AsSpan(2, 128 - 3 - data.Length).FillNonZero();
        data.CopyTo(block.AsSpan(128 - data.Length));

        var message = new BigInteger(block, isUnsigned: true, isBigEndian: true);
        var cipher = BigInteger.ModPow(message, key.Exponent, key.Modulus);
        var cipherBytes = cipher.ToByteArray(isUnsigned: true, isBigEndian: true);
        var padded = new byte[128];
        cipherBytes.CopyTo(padded.AsSpan(128 - cipherBytes.Length));
        return padded;
    }

    private static RsaPrivateKey GenerateTestKey()
    {
        var exponent = new BigInteger(3);
        while (true)
        {
            var prime = GeneratePrime(512);
            var other = GeneratePrime(512);
            while (prime == other)
                other = GeneratePrime(512);

            var phi = (prime - BigInteger.One) * (other - BigInteger.One);
            if (BigInteger.GreatestCommonDivisor(exponent, phi) != BigInteger.One)
                continue; // e must be coprime with phi for the inverse to exist

            var modulus = prime * other;
            return new RsaPrivateKey(modulus, exponent, ModInverse(exponent, phi));
        }
    }

    private static BigInteger GeneratePrime(int bits)
    {
        while (true)
        {
            var candidate = RandomBigInteger(bits) | BigInteger.One;
            if (IsProbablePrime(candidate, 40))
                return candidate;
        }
    }

    private static BigInteger RandomBigInteger(int bits)
    {
        var bytes = new byte[bits / 8];
        RandomNumberGenerator.Fill(bytes);
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true) | (BigInteger.One << (bits - 1));
    }

    private static bool IsProbablePrime(BigInteger value, int rounds)
    {
        if (value < 2) return false;
        if (value % 2 == 0) return value == 2;

        var d = value - 1;
        var s = 0;
        while (d.IsEven)
        {
            d >>= 1;
            s++;
        }

        var bytes = (value.GetBitLength() + 7) / 8;
        var buffer = new byte[bytes];
        for (var round = 0; round < rounds; round++)
        {
            RandomNumberGenerator.Fill(buffer);
            var a = new BigInteger(buffer, isUnsigned: true, isBigEndian: true) % (value - 2) + 2;
            var x = BigInteger.ModPow(a, d, value);
            if (x == 1 || x == value - 1)
                continue;

            var composite = true;
            for (var r = 1; r < s; r++)
            {
                x = BigInteger.ModPow(x, 2, value);
                if (x == value - 1)
                {
                    composite = false;
                    break;
                }
            }
            if (composite)
                return false;
        }
        return true;
    }

    private static BigInteger ModInverse(BigInteger a, BigInteger m)
    {
        BigInteger t = 0, newT = 1, r = m, newR = a;
        while (newR != 0)
        {
            var quotient = r / newR;
            (t, newT) = (newT, t - quotient * newT);
            (r, newR) = (newR, r - quotient * newR);
        }
        if (t < 0) t += m;
        return t;
    }
}

internal static class SpanExtensions
{
    public static void FillNonZero(this Span<byte> span)
    {
        foreach (ref var b in span)
            if (b == 0) b = 0x01;
    }
}
