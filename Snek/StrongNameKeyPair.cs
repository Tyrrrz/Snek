using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Snek;

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

    public static byte[] Generate(string seed)
    {
        var stream = new DeterministicByteStream(seed);
        var primeBits = KeySizeBits / 2;

        BigInteger p = GeneratePrime(stream, primeBits);

        BigInteger q;
        BigInteger e = 65537;
        BigInteger d;
        BigInteger n;
        while (true)
        {
            q = GeneratePrime(stream, primeBits);
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

    private static BigInteger GeneratePrime(DeterministicByteStream stream, int bits)
    {
        var byteLength = bits / 8;

        while (true)
        {
            var bytes = stream.Next(byteLength);

            // Force the two most significant bits so that the product of two such primes
            // always has the full expected bit length, and force the least significant bit
            // so that the candidate is odd.
            bytes[byteLength - 1] |= 0b1100_0000;
            bytes[0] |= 0x01;

            var candidate = FromBigEndianUnsigned(bytes);
            if (IsProbablyPrime(candidate, stream, byteLength))
                return candidate;
        }
    }

    private static bool IsProbablyPrime(
        BigInteger value,
        DeterministicByteStream stream,
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
                var candidate = FromBigEndianUnsigned(stream.Next(byteLength)) % (value - 3) + 2;
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

    // Interprets the provided bytes (in big-endian order) as a non-negative BigInteger.
    private static BigInteger FromBigEndianUnsigned(byte[] bigEndianBytes)
    {
        // BigInteger's byte array constructor expects little-endian, two's complement input.
        // An extra trailing zero byte guarantees the value is interpreted as non-negative.
        var littleEndianBytes = new byte[bigEndianBytes.Length + 1];
        for (var i = 0; i < bigEndianBytes.Length; i++)
            littleEndianBytes[i] = bigEndianBytes[bigEndianBytes.Length - 1 - i];

        return new BigInteger(littleEndianBytes);
    }

    // Converts a non-negative BigInteger to a fixed-length, big-endian, unsigned byte array,
    // as required by `RSAParameters`.
    private static byte[] ToFixedBigEndianBytes(BigInteger value, int length)
    {
        var littleEndianBytes = value.ToByteArray();
        var result = new byte[length];

        for (var i = 0; i < littleEndianBytes.Length; i++)
        {
            // Skip the extra zero sign byte that `BigInteger.ToByteArray()` may append.
            if (littleEndianBytes[i] == 0 && i >= length)
                continue;

            result[length - 1 - i] = littleEndianBytes[i];
        }

        return result;
    }

    // A deterministic pseudo-random byte generator, seeded from the provided string.
    // Used in place of a true RNG so that prime search and Miller-Rabin witness selection
    // (which both consume an arbitrary, a priori unknown amount of random-looking data)
    // produce the exact same sequence of bytes for the same seed on every run.
    // Internally, it hashes the seed together with an incrementing counter to produce an
    // unbounded stream of pseudo-random bytes (a simple counter-mode hash construction).
    private sealed class DeterministicByteStream
    {
        private readonly byte[] _seed;
        private long _counter;
        private byte[] _buffer = Array.Empty<byte>();
        private int _bufferPosition;

        public DeterministicByteStream(string seed) => _seed = Encoding.UTF8.GetBytes(seed);

        public byte[] Next(int count)
        {
            var result = new byte[count];
            var resultPosition = 0;

            while (resultPosition < count)
            {
                if (_bufferPosition >= _buffer.Length)
                {
                    var counterBytes = BitConverter.GetBytes(_counter);
                    var input = new byte[_seed.Length + counterBytes.Length];

                    Buffer.BlockCopy(_seed, 0, input, 0, _seed.Length);
                    Buffer.BlockCopy(counterBytes, 0, input, _seed.Length, counterBytes.Length);

                    _buffer = SHA256.HashData(input);
                    _bufferPosition = 0;
                    _counter++;
                }

                var chunkLength = Math.Min(
                    count - resultPosition,
                    _buffer.Length - _bufferPosition
                );
                Buffer.BlockCopy(_buffer, _bufferPosition, result, resultPosition, chunkLength);

                _bufferPosition += chunkLength;
                resultPosition += chunkLength;
            }

            return result;
        }
    }
}
