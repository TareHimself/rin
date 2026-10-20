using System.Numerics;

namespace Rin.Shade;

public abstract partial class Shader
{
    /// <summary>
    /// Discards the current fragment.
    /// </summary>
    [SlangStatement("discard;")]
    public static extern void Discard();

    /// <summary>
    /// Atomically adds <paramref name="value" /> to <paramref name="destination" />, returning the prior value in <paramref name="original" />.
    /// </summary>
    [SlangStatement("InterlockedAdd(@0, @1, @2);")]
    public static extern void InterlockedAdd(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedAdd(@0, @1, @2);")]
    public static extern void InterlockedAdd(ref int destination, int value, out int original);

    [SlangStatement("InterlockedAdd(@0, @1);")]
    public static extern void InterlockedAdd(ref uint destination, uint value);

    [SlangStatement("InterlockedAdd(@0, @1);")]
    public static extern void InterlockedAdd(ref int destination, int value);

    /// <summary>
    /// Atomically stores the smaller of <paramref name="destination" /> and <paramref name="value" />.
    /// </summary>
    [SlangStatement("InterlockedMin(@0, @1, @2);")]
    public static extern void InterlockedMin(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedMin(@0, @1, @2);")]
    public static extern void InterlockedMin(ref int destination, int value, out int original);

    [SlangStatement("InterlockedMin(@0, @1);")]
    public static extern void InterlockedMin(ref uint destination, uint value);

    [SlangStatement("InterlockedMin(@0, @1);")]
    public static extern void InterlockedMin(ref int destination, int value);

    /// <summary>
    /// Atomically stores the larger of <paramref name="destination" /> and <paramref name="value" />.
    /// </summary>
    [SlangStatement("InterlockedMax(@0, @1, @2);")]
    public static extern void InterlockedMax(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedMax(@0, @1, @2);")]
    public static extern void InterlockedMax(ref int destination, int value, out int original);

    [SlangStatement("InterlockedMax(@0, @1);")]
    public static extern void InterlockedMax(ref uint destination, uint value);

    [SlangStatement("InterlockedMax(@0, @1);")]
    public static extern void InterlockedMax(ref int destination, int value);

    /// <summary>
    /// Atomically ORs <paramref name="value" /> into <paramref name="destination" />.
    /// </summary>
    [SlangStatement("InterlockedOr(@0, @1, @2);")]
    public static extern void InterlockedOr(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedOr(@0, @1);")]
    public static extern void InterlockedOr(ref uint destination, uint value);

    /// <summary>
    /// Atomically ANDs <paramref name="value" /> into <paramref name="destination" />.
    /// </summary>
    [SlangStatement("InterlockedAnd(@0, @1, @2);")]
    public static extern void InterlockedAnd(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedAnd(@0, @1);")]
    public static extern void InterlockedAnd(ref uint destination, uint value);

    /// <summary>
    /// Atomically XORs <paramref name="value" /> into <paramref name="destination" />.
    /// </summary>
    [SlangStatement("InterlockedXor(@0, @1, @2);")]
    public static extern void InterlockedXor(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedXor(@0, @1);")]
    public static extern void InterlockedXor(ref uint destination, uint value);

    /// <summary>
    /// Atomically replaces <paramref name="destination" /> with <paramref name="value" />, returning the prior value in <paramref name="original" />.
    /// </summary>
    [SlangStatement("InterlockedExchange(@0, @1, @2);")]
    public static extern void InterlockedExchange(ref uint destination, uint value, out uint original);

    [SlangStatement("InterlockedExchange(@0, @1, @2);")]
    public static extern void InterlockedExchange(ref int destination, int value, out int original);

    [SlangStatement("InterlockedExchange(@0, @1, @2);")]
    public static extern void InterlockedExchange(ref float destination, float value, out float original);

    /// <summary>
    /// Atomically stores <paramref name="value" /> if <paramref name="destination" /> equals <paramref name="compare" />, returning the prior value in <paramref name="original" />.
    /// </summary>
    [SlangStatement("InterlockedCompareExchange(@0, @1, @2, @3);")]
    public static extern void InterlockedCompareExchange(ref uint destination, uint compare, uint value, out uint original);

    [SlangStatement("InterlockedCompareExchange(@0, @1, @2, @3);")]
    public static extern void InterlockedCompareExchange(ref int destination, int compare, int value, out int original);

    /// <summary>
    /// Waits until every thread in the group has reached this point and every group-shared write is visible to the group.
    /// </summary>
    [SlangStatement("GroupMemoryBarrierWithGroupSync();")]
    public static extern void GroupMemoryBarrierWithGroupSync();

    /// <summary>
    /// Makes this thread's group-shared writes visible to the group, without waiting for the other threads.
    /// </summary>
    [SlangStatement("GroupMemoryBarrier();")]
    public static extern void GroupMemoryBarrier();

    /// <summary>
    /// Waits until every thread in the group has reached this point and every device-memory write is visible to the group.
    /// </summary>
    [SlangStatement("DeviceMemoryBarrierWithGroupSync();")]
    public static extern void DeviceMemoryBarrierWithGroupSync();

    /// <summary>
    /// Makes this thread's device-memory writes visible, without waiting for the other threads.
    /// </summary>
    [SlangStatement("DeviceMemoryBarrier();")]
    public static extern void DeviceMemoryBarrier();

    /// <summary>
    /// Waits until every thread in the group has reached this point and every write, shared or device, is visible to the group.
    /// </summary>
    [SlangStatement("AllMemoryBarrierWithGroupSync();")]
    public static extern void AllMemoryBarrierWithGroupSync();

    /// <summary>
    /// Sum of the absolute screen-space derivatives of <paramref name="value" />.
    /// </summary>
    [SlangExpression("fwidth(@0)")]
    public static extern float Fwidth(float value);

    [SlangExpression("fwidth(@0)")]
    public static extern Vector2 Fwidth(Vector2 value);

    /// <summary>
    /// Reinterprets the bits of a float as a uint.
    /// </summary>
    [SlangExpression("asuint(@0)")]
    public static extern uint AsUInt(float value);

    /// <summary>
    /// Marks a descriptor index as non-uniform across the wave.
    /// </summary>
    [SlangExpression("NonUniformResourceIndex(@0)")]
    public static extern uint NonUniformResourceIndex(uint index);
}
