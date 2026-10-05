using System;
using System.Numerics;

namespace Snek.Utils.Extensions;

internal static class BigIntegerExtensions
{
    extension(BigInteger)
    {
        // This can be done via the BigInteger.ctor(byte[], bool, bool) constructor,
        // but that's only available on .NET Standard 2.1+ and cannot be polyfilled.
        public static BigInteger FromBytes(
            byte[] bytes,
            bool isUnsigned = false,
            bool isBigEndian = false
        )
        {
            var temp = (byte[])bytes.Clone();

            // BigInteger's byte array constructor expects little-endian, two's complement input.
            if (isBigEndian)
                Array.Reverse(temp);

            // An extra trailing zero byte guarantees the value is interpreted as non-negative.
            if (isUnsigned)
                Array.Resize(ref temp, temp.Length + 1);

            return new BigInteger(temp);
        }
    }
}
