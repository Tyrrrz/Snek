using System;

namespace Snek.Utils.Extensions;

internal static class ByteExtensions
{
    extension(byte[] bytes)
    {
        public byte[] PadToLength(int length)
        {
            if (bytes.Length > length)
            {
                throw new ArgumentException(
                    $"Array of length {bytes.Length} does not fit into {length} bytes."
                );
            }

            var result = new byte[length];
            bytes.CopyTo(result, length - bytes.Length);

            return result;
        }
    }
}
