namespace Rin.Core.Shared.Math;

/// <summary>
///     Deterministic random numbers (xoshiro256**, seeded through SplitMix64) whose <see cref="Current" /> state can be
///     saved and restored, so a restored generator draws the same sequence it drew after the save.
/// </summary>
/// <remarks>
///     A port of the public-domain reference implementations,
///     <see href="https://prng.di.unimi.it/xoshiro256starstar.c">xoshiro256starstar.c</see> and
///     <see href="https://prng.di.unimi.it/splitmix64.c">splitmix64.c</see>; <c>DeterministicRandomTests</c> checks it against
///     their outputs. A library is not used because the state must be saved, which the common ones hide.
/// </remarks>
public sealed class DeterministicRandom
{
    private State _state;

    public DeterministicRandom(ulong seed = 0) => Seed(seed);

    public void Seed(ulong seed)
    {
        var mix = seed;
        _state = new State(SplitMix(ref mix), SplitMix(ref mix), SplitMix(ref mix), SplitMix(ref mix));
    }

    public ulong NextUInt64()
    {
        ref var s = ref _state;
        var result = ulong.RotateLeft(s.S1 * 5, 7) * 9;
        var t = s.S1 << 17;
        s.S2 ^= s.S0;
        s.S3 ^= s.S1;
        s.S1 ^= s.S2;
        s.S0 ^= s.S3;
        s.S2 ^= t;
        s.S3 = ulong.RotateLeft(s.S3, 45);
        return result;
    }

    public uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>
    ///     A value in [0, <paramref name="maxExclusive" />), without modulo bias.
    /// </summary>
    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (int)NextBelow((uint)maxExclusive);
    }

    /// <summary>
    ///     A value in [<paramref name="minInclusive" />, <paramref name="maxExclusive" />).
    /// </summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxExclusive, minInclusive);
        return minInclusive + (int)NextBelow((uint)((long)maxExclusive - minInclusive));
    }

    /// <summary>
    ///     A value in [0, 1) with 24 random bits.
    /// </summary>
    public float NextFloat() => (NextUInt64() >> 40) * (1f / (1 << 24));

    public float NextFloat(float minInclusive, float maxExclusive) => minInclusive + (maxExclusive - minInclusive) * NextFloat();

    public bool NextBool() => (NextUInt64() >> 63) != 0;

    /// <summary>
    ///     The generator's state, to save and later assign back.
    /// </summary>
    public State Current
    {
        get => _state;
        set => _state = value;
    }

    // Lemire's multiply and reject.
    private uint NextBelow(uint bound)
    {
        var product = (ulong)NextUInt32() * bound;
        var low = (uint)product;
        if (low < bound)
        {
            var threshold = (0u - bound) % bound;
            while (low < threshold)
            {
                product = (ulong)NextUInt32() * bound;
                low = (uint)product;
            }
        }

        return (uint)(product >> 32);
    }

    private static ulong SplitMix(ref ulong x)
    {
        var z = x += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }

    public struct State(ulong s0, ulong s1, ulong s2, ulong s3)
    {
        internal ulong S0 = s0;
        internal ulong S1 = s1;
        internal ulong S2 = s2;
        internal ulong S3 = s3;
    }
}
