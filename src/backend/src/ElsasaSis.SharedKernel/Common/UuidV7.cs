using System.Security.Cryptography;

namespace ElsasaSis.SharedKernel.Common;

/// <summary>
/// Thread-safe, monotonic RFC 9562 UUID v7 generator.
/// </summary>
public static class UuidV7
{
    private static readonly object SyncRoot = new();
    private static long _lastTimestampMs;
    private static ushort _sequence;

    public static Guid NewGuid()
    {
        long timestampMs;
        ushort seq;

        lock (SyncRoot)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs > _lastTimestampMs)
            {
                _lastTimestampMs = nowMs;
                _sequence = (ushort)(RandomNumberGenerator.GetInt32(0, 4096) & 0x0FFF);
            }
            else
            {
                _sequence = (ushort)((_sequence + 1) & 0x0FFF);
                if (_sequence == 0)
                {
                    _lastTimestampMs++;
                }
            }

            timestampMs = _lastTimestampMs;
            seq = _sequence;
        }

        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes[8..]);

        // 48-bit big-endian Unix timestamp in milliseconds
        bytes[0] = (byte)(timestampMs >> 40);
        bytes[1] = (byte)(timestampMs >> 32);
        bytes[2] = (byte)(timestampMs >> 24);
        bytes[3] = (byte)(timestampMs >> 16);
        bytes[4] = (byte)(timestampMs >> 8);
        bytes[5] = (byte)timestampMs;

        // 4-bit version (7) + 12-bit monotonic sequence (rand_a)
        bytes[6] = (byte)(0x70 | ((seq >> 8) & 0x0F));
        bytes[7] = (byte)(seq & 0xFF);

        // 2-bit variant (10xxxxxx) + 62-bit random (rand_b)
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}
