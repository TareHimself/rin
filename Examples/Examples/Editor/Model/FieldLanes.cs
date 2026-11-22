using System.Numerics;
using System.Runtime.InteropServices;
using Rin.World;

namespace Examples.Editor.Model;

/// <summary>
///     Reads and writes one number of a field's bytes as a double, whatever its <see cref="FieldKind" />.
/// </summary>
public static class FieldLanes
{
    public static bool IsNumber(FieldKind kind) => kind is >= FieldKind.Int8 and <= FieldKind.Float64;

    public static bool IsInteger(FieldKind kind) => kind is >= FieldKind.Int8 and <= FieldKind.UInt64;

    public static double Read(FieldKind kind, ReadOnlySpan<byte> bytes) => kind switch
    {
        FieldKind.Int8 => (sbyte)bytes[0],
        FieldKind.UInt8 => bytes[0],
        FieldKind.Int16 => MemoryMarshal.Read<short>(bytes),
        FieldKind.UInt16 => MemoryMarshal.Read<ushort>(bytes),
        FieldKind.Int32 => MemoryMarshal.Read<int>(bytes),
        FieldKind.UInt32 => MemoryMarshal.Read<uint>(bytes),
        FieldKind.Int64 => MemoryMarshal.Read<long>(bytes),
        FieldKind.UInt64 => MemoryMarshal.Read<ulong>(bytes),
        FieldKind.Float32 => MemoryMarshal.Read<float>(bytes),
        FieldKind.Float64 => MemoryMarshal.Read<double>(bytes),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a number")
    };

    /// <summary>
    ///     Integers are rounded and clamped to their range, so a dragged or typed value never wraps around.
    /// </summary>
    public static void Write(FieldKind kind, Span<byte> bytes, double value)
    {
        if (IsInteger(kind)) value = Math.Round(value);
        switch (kind)
        {
            case FieldKind.Int8: bytes[0] = (byte)(sbyte)Math.Clamp(value, sbyte.MinValue, sbyte.MaxValue); break;
            case FieldKind.UInt8: bytes[0] = (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue); break;
            case FieldKind.Int16: Put(bytes, (short)Math.Clamp(value, short.MinValue, short.MaxValue)); break;
            case FieldKind.UInt16: Put(bytes, (ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue)); break;
            case FieldKind.Int32: Put(bytes, (int)Math.Clamp(value, int.MinValue, int.MaxValue)); break;
            case FieldKind.UInt32: Put(bytes, (uint)Math.Clamp(value, uint.MinValue, uint.MaxValue)); break;
            case FieldKind.Int64: Put(bytes, (long)Math.Clamp(value, long.MinValue, long.MaxValue)); break;
            case FieldKind.UInt64: Put(bytes, (ulong)Math.Clamp(value, ulong.MinValue, ulong.MaxValue)); break;
            case FieldKind.Float32: Put(bytes, (float)value); break;
            case FieldKind.Float64: Put(bytes, value); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a number");
        }
    }

    /// <summary>
    ///     Pitch about X, yaw about Y and roll about Z in degrees, the inverse of <see cref="FromEuler" />.
    /// </summary>
    public static Vector3 ToEuler(Quaternion q)
    {
        var pitch = MathF.Asin(float.Clamp(2f * (q.W * q.X - q.Y * q.Z), -1f, 1f));
        var yaw = MathF.Atan2(2f * (q.W * q.Y + q.X * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
        var roll = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.X * q.X + q.Z * q.Z));
        return new Vector3(float.RadiansToDegrees(pitch), float.RadiansToDegrees(yaw), float.RadiansToDegrees(roll));
    }

    public static Quaternion FromEuler(Vector3 degrees) =>
        Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(degrees.Y), float.DegreesToRadians(degrees.X),
            float.DegreesToRadians(degrees.Z));

    private static void Put<T>(Span<byte> bytes, T value) where T : unmanaged => MemoryMarshal.Write(bytes, in value);
}
