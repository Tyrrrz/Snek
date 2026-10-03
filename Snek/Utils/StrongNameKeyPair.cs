using System;
using System.Numerics;
using System.Security.Cryptography;
using Snek.Utils.Extensions;

namespace Snek.Utils;

// Deterministically generates an RSA key pair from a string seed, such that the same seed
// always produces the same key pair, while different seeds produce different key pairs.
// This algorithm is considered frozen: changing it would silently change the resulting key
// (and thus the public key token) for every existing seed, which would be a breaking change.
internal static class StrongNameKeyPair
{
    // Matches the key size used by `sn -k` for the strong name key pairs historically shipped
    // with this package.
    private const int KeySizeBits = 1024;

    private static readonly int[] SmallPrimes =
    {
        2,
        3,
        5,
        7,
        11,
        13,
        17,
        19,
        23,
        29,
        31,
        37,
        41,
        43,
        47,
        53,
        59,
        61,
        67,
        71,
        73,
        79,
        83,
        89,
        97,
    };

    private static BigInteger ModInverse(BigInteger value, BigInteger modulus)
    {
        var originalModulus = modulus;
        BigInteger y = 0;
        BigInteger x = 1;

        var a = value % modulus;
        if (a < 0)
            a += modulus;

        while (a > 1)
        {
            var quotient = a / modulus;

            (modulus, a) = (a % modulus, modulus);
            (y, x) = (x - quotient * y, y);
        }

        if (x < 0)
            x += originalModulus;

        return x;
    }

    private static BigInteger GeneratePrime(SeededRandom random, int bits)
    {
        var byteLength = bits / 8;

        while (true)
        {
            var bytes = random.NextBytes(byteLength);

            // Force the two most significant bits so that the product of two such primes
            // always has the full expected bit length, and force the least significant bit
            // so that the candidate is odd.
            bytes[0] |= 0b1100_0000;
            bytes[byteLength - 1] |= 0x01;

            var candidate = BigInteger.FromBigEndianUnsigned(bytes);
            if (IsProbablyPrime(candidate, random, byteLength))
                return candidate;
        }
    }

    private static bool IsProbablyPrime(
        BigInteger value,
        SeededRandom random,
        int byteLength,
        int rounds = 32
    )
    {
        if (value < 2)
            return false;

        foreach (var smallPrime in SmallPrimes)
        {
            if (value == smallPrime)
                return true;

            if (value % smallPrime == 0)
                return false;
        }

        var d = value - 1;
        var r = 0;
        while (d.IsEven)
        {
            d /= 2;
            r++;
        }

        for (var i = 0; i < rounds; i++)
        {
            BigInteger witness;
            while (true)
            {
                var candidate =
                    BigInteger.FromBigEndianUnsigned(random.NextBytes(byteLength)) % (value - 3)
                    + 2;
                if (candidate >= 2 && candidate <= value - 2)
                {
                    witness = candidate;
                    break;
                }
            }

            var x = BigInteger.ModPow(witness, d, value);
            if (x == 1 || x == value - 1)
                continue;

            var isComposite = true;
            for (var j = 0; j < r - 1; j++)
            {
                x = BigInteger.ModPow(x, 2, value);
                if (x == value - 1)
                {
                    isComposite = false;
                    break;
                }
            }

            if (isComposite)
                return false;
        }

        return true;
    }

    // Converts a non-negative BigInteger to a fixed-length, big-endian, unsigned byte array,
    // as required by `RSAParameters`.
    private static byte[] ToFixedBigEndianBytes(BigInteger value, int length)
    {
        var bigEndianBytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var result = new byte[length];
        Buffer.BlockCopy(
            bigEndianBytes,
            0,
            result,
            length - bigEndianBytes.Length,
            bigEndianBytes.Length
        );

        return result;
    }

    public static byte[] Generate(string seed)
    {
        var random = new SeededRandom(seed);
        var primeBits = KeySizeBits / 2;

        var p = GeneratePrime(random, primeBits);

        BigInteger q;
        BigInteger e = 65537;
        BigInteger d;
        BigInteger n;
        while (true)
        {
            q = GeneratePrime(random, primeBits);
            if (q == p)
                continue;

            var (pp, qq) = p > q ? (p, q) : (q, p);
            var phi = (pp - 1) * (qq - 1);

            if (BigInteger.GreatestCommonDivisor(e, phi) != 1)
                continue;

            p = pp;
            q = qq;
            n = pp * qq;
            d = ModInverse(e, phi);

            break;
        }

        var dp = d % (p - 1);
        var dq = d % (q - 1);
        var qInv = ModInverse(q, p);

        var parameters = new RSAParameters
        {
            Modulus = ToFixedBigEndianBytes(n, KeySizeBits / 8),
            Exponent = ToFixedBigEndianBytes(e, 3),
            D = ToFixedBigEndianBytes(d, KeySizeBits / 8),
            P = ToFixedBigEndianBytes(p, primeBits / 8),
            Q = ToFixedBigEndianBytes(q, primeBits / 8),
            DP = ToFixedBigEndianBytes(dp, primeBits / 8),
            DQ = ToFixedBigEndianBytes(dq, primeBits / 8),
            InverseQ = ToFixedBigEndianBytes(qInv, primeBits / 8),
        };

        using var rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(parameters);

        return rsa.ExportCspBlob(true);
    }
}
