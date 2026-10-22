using System.Numerics;
using System.Security.Cryptography;

namespace Habbo.Protocol.Crypto;

/// <summary>Classic Diffie-Hellman over server-chosen parameters (Habbo handshake). The server picks a random prime and
/// generator for every connection, as Habbo does: fixed ones would let anyone who learns one session's key material
/// reuse the work for every other session.</summary>
public static class DiffieHellman
{
    /// <summary>The most decimal digits a value the client handles may have: the prime and generator arrive RSA type-1
    /// signed, and the client's public key goes back RSA type-2 encrypted, each in one 128-byte block less 11 bytes
    /// of padding.</summary>
    public const int ClientDigitLimit = 117;

    /// <summary>The client tries for a public key of at least this many digits (up to ten times) before it settles
    /// for the longest it got: a prime this long or longer lets it.</summary>
    public const int ClientPreferredDigits = 64;

    /// <summary>Miller-Rabin rounds: a composite passes all of them with probability below 4^-40.</summary>
    private const int PrimalityRounds = 40;

    /// <summary>The largest prime size whose every value fits the client's limit.</summary>
    public static int MaxPrimeBits { get; } = (int)Math.Floor((ClientDigitLimit - 1) / Math.Log10(2));

    /// <summary>A random prime of exactly <paramref name="bits"/> bits and a random generator below it (2 to p-2).</summary>
    public static (BigInteger Prime, BigInteger Generator) GenerateParameters(int bits)
    {
        if (bits < 16 || bits > MaxPrimeBits)
            throw new ArgumentOutOfRangeException(nameof(bits), bits, $"a prime the client accepts has 16 to {MaxPrimeBits} bits");

        var prime = RandomPrime(bits);
        var generator = RandomBetween(2, prime - 2);
        return (prime, generator);
    }

    /// <summary>Creates a private key matching the client's: a random 30-byte value.</summary>
    public static BigInteger GeneratePrivateKey(int bytes = 30)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        return new BigInteger(buffer, isUnsigned: true, isBigEndian: true);
    }

    public static BigInteger ComputePublicKey(BigInteger prime, BigInteger generator, BigInteger privateKey)
        => BigInteger.ModPow(generator, privateKey, prime);

    /// <summary>Whether a peer's public key is one a real exchange can produce: 1 &lt; y &lt; p - 1. Anything else (0, 1,
    /// p - 1, negative or past the prime) forces the shared secret to a value anyone can guess.</summary>
    public static bool IsValidPublicKey(BigInteger prime, BigInteger peerPublicKey)
        => peerPublicKey > 1 && peerPublicKey < prime - 1;

    /// <summary>The secret shared with the peer; throws when its public key is not a valid one.</summary>
    public static BigInteger ComputeSharedSecret(BigInteger prime, BigInteger peerPublicKey, BigInteger privateKey)
    {
        if (!IsValidPublicKey(prime, peerPublicKey))
            throw new ArgumentOutOfRangeException(nameof(peerPublicKey), "a public key must be above 1 and below p - 1");

        return BigInteger.ModPow(peerPublicKey, privateKey, prime);
    }

    /// <summary>Derives the RC4 key exactly like the client: uppercase hex of the shared secret with
    /// leading zeros dropped, then a leading "0" nibble when the hex length is odd.</summary>
    public static byte[] DeriveRc4Key(BigInteger sharedSecret)
    {
        // BigInteger.ToString("X") prepends a sign nibble when the top nibble is >= 8
        // (255 -> "0FF"); the client's toRadix(16) never does, so strip it first.
        var hex = sharedSecret.ToString("X").TrimStart('0');
        if (hex.Length == 0)
            hex = "0";
        if (hex.Length % 2 == 1)
            hex = "0" + hex;
        return Convert.FromHexString(hex);
    }

    /// <summary>Whether <paramref name="n"/> is prime, by Miller-Rabin with random bases.</summary>
    public static bool IsProbablePrime(BigInteger n, int rounds = PrimalityRounds)
    {
        if (n < 2)
            return false;
        foreach (var small in SmallPrimes)
        {
            if (n == small)
                return true;
            if (n % small == 0)
                return false;
        }

        // n - 1 = d * 2^s with d odd
        var d = n - 1;
        var s = 0;
        while (d.IsEven)
        {
            d >>= 1;
            s++;
        }

        for (var round = 0; round < rounds; round++)
        {
            var x = BigInteger.ModPow(RandomBetween(2, n - 2), d, n);
            if (x == 1 || x == n - 1)
                continue;

            var witness = true;
            for (var i = 1; i < s && witness; i++)
            {
                x = BigInteger.ModPow(x, 2, n);
                if (x == n - 1)
                    witness = false;
            }

            if (witness)
                return false;
        }

        return true;
    }

    private static readonly int[] SmallPrimes = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71];

    /// <summary>An odd number with its top bit set, tried until one is prime.</summary>
    private static BigInteger RandomPrime(int bits)
    {
        var bytes = new byte[(bits + 7) / 8];
        var topBit = (bits - 1) % 8;
        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            bytes[0] &= (byte)((1 << (topBit + 1)) - 1);   // no bits above the size
            bytes[0] |= (byte)(1 << topBit);                // exactly this size
            bytes[^1] |= 1;                                 // odd
            var candidate = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
            if (IsProbablePrime(candidate))
                return candidate;
        }
    }

    /// <summary>A uniformly random number from <paramref name="min"/> to <paramref name="max"/>, both included.</summary>
    private static BigInteger RandomBetween(BigInteger min, BigInteger max)
    {
        var range = max - min + 1;
        var bytes = new byte[range.GetByteCount(isUnsigned: true)];
        var bits = (int)range.GetBitLength();
        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
            value &= (BigInteger.One << bits) - 1;          // drawn from the range's bit length, rejected when past it
            if (value < range)
                return min + value;
        }
    }
}
