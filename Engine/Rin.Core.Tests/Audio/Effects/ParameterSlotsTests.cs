using System.Runtime.InteropServices;
using Rin.Core.Audio.Effects;

namespace Rin.Core.Tests.Audio.Effects;

public class ParameterSlotsTests
{
    private struct Wide
    {
        public long A, B, C, D, E, F, G, H;

        public static Wide Filled(long value) => new() { A = value, B = value, C = value, D = value, E = value, F = value, G = value, H = value };

        public readonly bool IsUniform => A == B && B == C && C == D && D == E && E == F && F == G && G == H;
    }

    [Test]
    public unsafe void Reads_the_initial_value_before_any_write()
    {
        var slots = new ParameterSlots<Wide>(Wide.Filled(7));

        Assert.That(ParameterSlots<Wide>.Read(&slots).A, Is.EqualTo(7));
    }

    [Test]
    public unsafe void Reader_sees_the_latest_write_and_keeps_it_until_the_next()
    {
        var slots = new ParameterSlots<Wide>(default);

        ParameterSlots<Wide>.Write(&slots, Wide.Filled(1));
        ParameterSlots<Wide>.Write(&slots, Wide.Filled(2));
        ParameterSlots<Wide>.Write(&slots, Wide.Filled(3));

        Assert.That(ParameterSlots<Wide>.Read(&slots).A, Is.EqualTo(3));
        Assert.That(ParameterSlots<Wide>.Read(&slots).A, Is.EqualTo(3));

        ParameterSlots<Wide>.Write(&slots, Wide.Filled(4));

        Assert.That(ParameterSlots<Wide>.Read(&slots).A, Is.EqualTo(4));
    }

    [Test]
    public unsafe void Concurrent_writer_never_tears_a_read()
    {
        var memory = (ParameterSlots<Wide>*)NativeMemory.AlignedAlloc((nuint)sizeof(ParameterSlots<Wide>), 16);
        *memory = new ParameterSlots<Wide>(Wide.Filled(0));
        var running = true;
        var torn = 0;
        long lastSeen = 0;
        var regressed = 0;

        var writer = Task.Run(() =>
        {
            for (long i = 1; i <= 2_000_000; i++) ParameterSlots<Wide>.Write(memory, Wide.Filled(i));
            Volatile.Write(ref running, false);
        });

        while (Volatile.Read(ref running))
        {
            ref readonly var value = ref ParameterSlots<Wide>.Read(memory);
            if (!value.IsUniform) torn++;
            if (value.A < lastSeen) regressed++;
            lastSeen = value.A;
        }

        writer.Wait();
        var final = ParameterSlots<Wide>.Read(memory).A;
        NativeMemory.AlignedFree(memory);

        Assert.Multiple(() =>
        {
            Assert.That(torn, Is.Zero);
            Assert.That(regressed, Is.Zero);
            Assert.That(final, Is.EqualTo(2_000_000));
        });
    }
}
