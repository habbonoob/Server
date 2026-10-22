using System.Numerics;
using Habbo.Protocol.Crypto;

namespace Habbo.Protocol.Tests;

public class DiffieHellmanTests
{
    private const int Bits = 256;

    [Fact]
    public void SharedSecretsAgree()
    {
        var (prime, generator) = DiffieHellman.GenerateParameters(Bits);

        var alicePrivate = DiffieHellman.GeneratePrivateKey();
        var bobPrivate = DiffieHellman.GeneratePrivateKey();

        var alicePublic = DiffieHellman.ComputePublicKey(prime, generator, alicePrivate);
        var bobPublic = DiffieHellman.ComputePublicKey(prime, generator, bobPrivate);

        var aliceShared = DiffieHellman.ComputeSharedSecret(prime, bobPublic, alicePrivate);
        var bobShared = DiffieHellman.ComputeSharedSecret(prime, alicePublic, bobPrivate);

        Assert.Equal(aliceShared, bobShared);
    }

    [Fact]
    public void APublicKeyOutsideOneToPrimeLessOneIsRefused()
    {
        var (prime, _) = DiffieHellman.GenerateParameters(Bits);
        var privateKey = DiffieHellman.GeneratePrivateKey();

        foreach (var key in new[] { BigInteger.MinusOne, BigInteger.Zero, BigInteger.One, prime - 1, prime, prime + 2 })
        {
            Assert.False(DiffieHellman.IsValidPublicKey(prime, key));
            Assert.Throws<ArgumentOutOfRangeException>(() => DiffieHellman.ComputeSharedSecret(prime, key, privateKey));
        }

        Assert.True(DiffieHellman.IsValidPublicKey(prime, 2));
        Assert.True(DiffieHellman.IsValidPublicKey(prime, prime - 2));
    }

    [Fact]
    public void ParametersAreRandomPrimesTheClientAccepts()
    {
        var (prime, generator) = DiffieHellman.GenerateParameters(Bits);
        var (otherPrime, otherGenerator) = DiffieHellman.GenerateParameters(Bits);

        Assert.NotEqual(prime, otherPrime);
        Assert.NotEqual(generator, otherGenerator);
        Assert.Equal(Bits, (int)prime.GetBitLength());
        Assert.True(DiffieHellman.IsProbablePrime(prime));

        // what the client checks (IncomingMessages.onInitDiffieHandshake): p > 2, g < p, g != p
        Assert.True(prime > 2);
        Assert.True(generator > 1 && generator < prime - 1);

        // long enough for the public key the client tries for, short enough for its RSA blocks
        Assert.InRange(prime.ToString().Length, DiffieHellman.ClientPreferredDigits, DiffieHellman.ClientDigitLimit);
        Assert.True(generator.ToString().Length <= DiffieHellman.ClientDigitLimit);
    }

    [Fact]
    public void TheLargestPrimeSizeStillFitsTheClient()
    {
        var largest = (BigInteger.One << DiffieHellman.MaxPrimeBits) - 1;
        Assert.True(largest.ToString().Length <= DiffieHellman.ClientDigitLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => DiffieHellman.GenerateParameters(DiffieHellman.MaxPrimeBits + 1));
    }

    [Fact]
    public void PrimalityTestTellsPrimesFromComposites()
    {
        // 2^127 - 1 is a Mersenne prime; times 3 it is not, nor is a Carmichael number (561 = 3 * 11 * 17)
        var mersenne = (BigInteger.One << 127) - 1;
        Assert.True(DiffieHellman.IsProbablePrime(mersenne));
        Assert.False(DiffieHellman.IsProbablePrime(mersenne * 3));
        Assert.False(DiffieHellman.IsProbablePrime(561));
        Assert.False(DiffieHellman.IsProbablePrime(1));
        Assert.True(DiffieHellman.IsProbablePrime(2));
    }

    [Fact]
    public void Rc4KeyDerivationMatchesClientSemantics()
    {
        // 0x1234 -> hex "1234" -> 12 34
        Assert.Equal(new byte[] { 0x12, 0x34 }, DiffieHellman.DeriveRc4Key(new BigInteger(0x1234)));

        // 0x1 -> hex "1" (odd length) -> "01" -> 01
        Assert.Equal(new byte[] { 0x01 }, DiffieHellman.DeriveRc4Key(BigInteger.One));

        // 0x0ABC -> hex "ABC" (leading zeros dropped, odd length) -> "0ABC" -> 0A BC
        Assert.Equal(new byte[] { 0x0A, 0xBC }, DiffieHellman.DeriveRc4Key(new BigInteger(0x0ABC)));

        // REGRESSION: a top nibble >= 8 made .NET emit a sign nibble ("0FF"), producing an
        // extra 00 key byte the client never has -> RC4 streams diverge (~0.7% of logins;
        // the server then reads the first encrypted frame as garbage).
        Assert.Equal(new byte[] { 0xFF }, DiffieHellman.DeriveRc4Key(new BigInteger(0xFF)));
        Assert.Equal(new byte[] { 0x80, 0x00 }, DiffieHellman.DeriveRc4Key(new BigInteger(0x8000)));
        Assert.Equal(new byte[] { 0x08, 0xFF }, DiffieHellman.DeriveRc4Key(new BigInteger(0x8FF)));
    }
}
