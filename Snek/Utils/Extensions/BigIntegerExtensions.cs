using System.Numerics;

namespace Snek.Utils.Extensions;

internal static class BigIntegerExtensions
{
    // Interprets the provided bytes (in big-endian order) as a non-negative BigInteger.
    public static BigInteger FromBigEndianUnsigned(this byte[] bigEndianBytes)
    {
        // BigInteger's byte array constructor expects little-endian, two's complement input.
        // An extra trailing zero byte guarantees the value is interpreted as non-negative.
        var littleEndianBytes = new byte[bigEndianBytes.Length + 1];
        for (var i = 0; i < bigEndianBytes.Length; i++)
            littleEndianBytes[i] = bigEndianBytes[bigEndianBytes.Length - 1 - i];

        return new BigInteger(littleEndianBytes);
    }
}
