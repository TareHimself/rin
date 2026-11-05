using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.World;

/// <summary>
///     A reusable buffer for <see cref="World.SaveSnapshot" /> and <see cref="World.RestoreSnapshot" />. The buffer grows
///     to the largest state saved into it and is reused after that, so a rollback loop saving into the same snapshots
///     does not allocate.
/// </summary>
/// <remarks>
///     The bytes are only meaningful to a <see cref="World" /> in the same process with the same features registered.
/// </remarks>
public sealed class WorldSnapshot
{
    private byte[] _buffer;

    public WorldSnapshot(int initialCapacity = 4096) => _buffer = new byte[int.Max(initialCapacity, 64)];

    public int Length { get; private set; }

    public bool IsEmpty => Length == 0;

    /// <summary>
    ///     The tick the snapshot was saved at.
    /// </summary>
    public long TickIndex { get; internal set; }

    public ReadOnlySpan<byte> Bytes => _buffer.AsSpan(0, Length);

    /// <summary>
    ///     A 64-bit FNV-1a hash of <see cref="Bytes" />, stable across processes, for comparing worlds tick by tick.
    /// </summary>
    public ulong ComputeHash()
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;

        var bytes = Bytes;
        var hash = offset;
        var words = MemoryMarshal.Cast<byte, ulong>(bytes);
        for (var i = 0; i < words.Length; i++) hash = (hash ^ words[i]) * prime;
        for (var i = words.Length * sizeof(ulong); i < bytes.Length; i++) hash = (hash ^ bytes[i]) * prime;
        return hash;
    }

    public void Clear()
    {
        Length = 0;
        TickIndex = 0;
    }

    internal void Write<T>(in T value) where T : unmanaged => MemoryMarshal.Write(Reserve(Unsafe.SizeOf<T>()), in value);

    internal void WriteSpan<T>(ReadOnlySpan<T> values) where T : unmanaged =>
        MemoryMarshal.AsBytes(values).CopyTo(Reserve(values.Length * Unsafe.SizeOf<T>()));

    internal void WriteBytes(ReadOnlySpan<byte> bytes) => bytes.CopyTo(Reserve(bytes.Length));

    internal SnapshotReader CreateReader() => new(Bytes);

    private Span<byte> Reserve(int count)
    {
        var end = Length + count;
        if (end > _buffer.Length) Array.Resize(ref _buffer, int.Max(end, _buffer.Length * 2));
        var span = _buffer.AsSpan(Length, count);
        Length = end;
        return span;
    }
}

internal ref struct SnapshotReader(ReadOnlySpan<byte> bytes)
{
    private readonly ReadOnlySpan<byte> _bytes = bytes;
    private int _position;

    public bool IsAtEnd => _position == _bytes.Length;

    public T Read<T>() where T : unmanaged => MemoryMarshal.Read<T>(ReadBytes(Unsafe.SizeOf<T>()));

    public void ReadSpan<T>(Span<T> destination) where T : unmanaged =>
        ReadBytes(destination.Length * Unsafe.SizeOf<T>()).CopyTo(MemoryMarshal.AsBytes(destination));

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var span = _bytes.Slice(_position, count);
        _position += count;
        return span;
    }
}
