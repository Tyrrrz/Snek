using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Snek.Utils;

// Like System.Random, but with a deterministic implementation based on a string seed
internal class SeededRandom(string seed)
{
    // Seed bytes followed by room for the big-endian block counter
    private readonly byte[] _input = [.. Encoding.UTF8.GetBytes(seed), .. new byte[sizeof(long)]];

    private long _counter;
    private byte[] _buffer = [];
    private int _bufferPosition;

    public byte[] NextBytes(int count)
    {
        var result = new byte[count];
        var resultPosition = 0;

        while (resultPosition < count)
        {
            if (_bufferPosition >= _buffer.Length)
            {
                BinaryPrimitives.WriteInt64BigEndian(
                    _input.AsSpan(_input.Length - sizeof(long)),
                    _counter
                );

                _buffer = SHA256.HashData(_input);
                _bufferPosition = 0;
                _counter++;
            }

            var chunkLength = Math.Min(count - resultPosition, _buffer.Length - _bufferPosition);
            _buffer.AsSpan(_bufferPosition, chunkLength).CopyTo(result.AsSpan(resultPosition));

            _bufferPosition += chunkLength;
            resultPosition += chunkLength;
        }

        return result;
    }
}
