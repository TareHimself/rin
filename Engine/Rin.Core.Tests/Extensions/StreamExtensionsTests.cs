using Rin.Core.Extensions;

namespace Rin.Core.Tests.Extensions;

public class StreamExtensionsTests
{
    [TestCase(0UL)]
    [TestCase(12UL)]
    [TestCase(ulong.MaxValue)]
    public void WriteUInt64_RoundTripsThroughReadUInt64(ulong value)
    {
        using var stream = new MemoryStream();
        stream.Write(value);
        stream.Position = 0;

        Assert.Multiple(() =>
        {
            Assert.That(stream.Length, Is.EqualTo(sizeof(ulong)));
            Assert.That(stream.ReadUInt64(), Is.EqualTo(value));
        });
    }
}
