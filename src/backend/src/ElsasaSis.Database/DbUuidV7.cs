using System.Security.Cryptography;

namespace ElsasaSis.Database;

/// <summary>
/// Local RFC 9562 UUID v7 generator so ElsasaSis.Database stays free of module/kernel references.
/// </summary>
internal static class DbUuidV7
{
    public static Guid NewGuid()
    {
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes[6..]);

        bytes[0] = (byte)(timestampMs >> 40);
        bytes[1] = (byte)(timestampMs >> 32);
        bytes[2] = (byte)(timestampMs >> 24);
        bytes[3] = (byte)(timestampMs >> 16);
        bytes[4] = (byte)(timestampMs >> 8);
        bytes[5] = (byte)timestampMs;

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}
