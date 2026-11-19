using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.SharedKernel;

public sealed class UuidV7Tests
{
    [Fact]
    public void NewGuid_SetsRfc9562Version7AndVariantBits()
    {
        var guid = UuidV7.NewGuid();
        var bytes = guid.ToByteArray(bigEndian: true);

        // Version nibble in byte 6 must be 0x7 (0111)
        var version = (bytes[6] & 0xF0) >> 4;
        Assert.Equal(7, version);

        // Variant bits in byte 8 must be 0b10 (RFC 9562)
        var variant = (bytes[8] & 0xC0) >> 6;
        Assert.Equal(0b10, variant);
    }

    [Fact]
    public void NewGuid_IsStrictlyMonotonicAcrossRapidCalls()
    {
        const int count = 5000;
        var guids = new Guid[count];

        for (var i = 0; i < count; i++)
        {
            guids[i] = UuidV7.NewGuid();
        }

        for (var i = 1; i < count; i++)
        {
            var prev = guids[i - 1].ToString("D");
            var curr = guids[i].ToString("D");
            Assert.True(
                string.CompareOrdinal(prev, curr) < 0,
                $"Expected {prev} < {curr} at index {i}");
        }
    }
}
