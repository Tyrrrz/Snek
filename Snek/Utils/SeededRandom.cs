using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Snek.Utils;

// Like System.Random, but with a deterministic implementation based on a string seed
internal class SeededRandom(string seed)
{
    private readonly byte[] _seed = Encoding.UTF8.GetBytes(seed);
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
                var counterBytes = new byte[sizeof(long)];
                BinaryPrimitives.WriteInt64BigEndian(counterBytes, _counter);
                var input = new byte[_seed.Length + counterBytes.Length];

                Buffer.BlockCopy(_seed, 0, input, 0, _seed.Length);
                Buffer.BlockCopy(counterBytes, 0, input, _seed.Length, counterBytes.Length);

                _buffer = SHA256.HashData(input);
                _bufferPosition = 0;
                _counter++;
            }

            var chunkLength = Math.Min(count - resultPosition, _buffer.Length - _bufferPosition);
            Buffer.BlockCopy(_buffer, _bufferPosition, result, resultPosition, chunkLength);

            _bufferPosition += chunkLength;
            resultPosition += chunkLength;
        }

        return result;
    }
}
