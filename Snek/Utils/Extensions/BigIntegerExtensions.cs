using System.Numerics;

namespace Snek.Utils.Extensions;

internal static class BigIntegerExtensions
{
    extension(BigInteger)
    {
        // This can be done via the BigInteger.ctor(byte[], bool, bool) constructor,
        // but that's only available on .NET Standard 2.1+ and cannot be polyfilled.
        public static BigInteger FromBigEndianUnsigned(byte[] bigEndianBytes)
        {
            // BigInteger's byte array constructor expects little-endian, two's complement input.
            // An extra trailing zero byte guarantees the value is interpreted as non-negative.
            var littleEndianBytes = new byte[bigEndianBytes.Length + 1];
            for (var i = 0; i < bigEndianBytes.Length; i++)
                littleEndianBytes[i] = bigEndianBytes[bigEndianBytes.Length - 1 - i];

            return new BigInteger(littleEndianBytes);
        }
    }
}
