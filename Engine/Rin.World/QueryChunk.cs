using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}(int)" /> a row, or
///     <see cref="WriteAll{T}" /> every row, to change them, so the write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1>
    where T1 : unmanaged, IComponentData
{
    private readonly ChunkView _view;
    private readonly ChunkColumn<T1> _column1;

    internal QueryChunk(ChunkView view, ComponentId id1)
    {
        _view = view;
        _column1 = view.Column<T1>(id1);
    }

    public ReadOnlySpan<T1> Components1 => _column1.Components;

    public ChunkEntities Entities => _view.Entities;

    public int Length => _view.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _column1.Write(index, _view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Every row of <typeparamref name="T" />, one of the queried components, stamped as changed at once. Faster than
    ///     <see cref="Write{T}(int)" /> per row, but a change query then sees every row as changed.
    /// </summary>
    public Span<T> WriteAll<T>() where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return MemoryMarshal.Cast<T1, T>(_column1.WriteAll(_view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _column1.HasChanged(index, _view.Ticks);
        throw ChunkChangeTicks.NotQueried<T>();
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ChunkEntities entities)
    {
        components1 = Components1;
        entities = Entities;
    }
}

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}(int)" /> a row, or
///     <see cref="WriteAll{T}" /> every row, to change them, so the write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
{
    private readonly ChunkView _view;
    private readonly ChunkColumn<T1> _column1;
    private readonly ChunkColumn<T2> _column2;

    internal QueryChunk(ChunkView view, ComponentId id1, ComponentId id2)
    {
        _view = view;
        _column1 = view.Column<T1>(id1);
        _column2 = view.Column<T2>(id2);
    }

    public ReadOnlySpan<T1> Components1 => _column1.Components;

    public ReadOnlySpan<T2> Components2 => _column2.Components;

    public ChunkEntities Entities => _view.Entities;

    public int Length => _view.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _column1.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _column2.Write(index, _view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Every row of <typeparamref name="T" />, one of the queried components, stamped as changed at once. Faster than
    ///     <see cref="Write{T}(int)" /> per row, but a change query then sees every row as changed.
    /// </summary>
    public Span<T> WriteAll<T>() where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return MemoryMarshal.Cast<T1, T>(_column1.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T2)) return MemoryMarshal.Cast<T2, T>(_column2.WriteAll(_view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _column1.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T2)) return _column2.HasChanged(index, _view.Ticks);
        throw ChunkChangeTicks.NotQueried<T>();
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2)
    {
        components1 = Components1;
        components2 = Components2;
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        entities = Entities;
    }
}

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}(int)" /> a row, or
///     <see cref="WriteAll{T}" /> every row, to change them, so the write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2, T3>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
{
    private readonly ChunkView _view;
    private readonly ChunkColumn<T1> _column1;
    private readonly ChunkColumn<T2> _column2;
    private readonly ChunkColumn<T3> _column3;

    internal QueryChunk(ChunkView view, ComponentId id1, ComponentId id2, ComponentId id3)
    {
        _view = view;
        _column1 = view.Column<T1>(id1);
        _column2 = view.Column<T2>(id2);
        _column3 = view.Column<T3>(id3);
    }

    public ReadOnlySpan<T1> Components1 => _column1.Components;

    public ReadOnlySpan<T2> Components2 => _column2.Components;

    public ReadOnlySpan<T3> Components3 => _column3.Components;

    public ChunkEntities Entities => _view.Entities;

    public int Length => _view.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _column1.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _column2.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T3)) return ref Unsafe.As<T3, T>(ref _column3.Write(index, _view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Every row of <typeparamref name="T" />, one of the queried components, stamped as changed at once. Faster than
    ///     <see cref="Write{T}(int)" /> per row, but a change query then sees every row as changed.
    /// </summary>
    public Span<T> WriteAll<T>() where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return MemoryMarshal.Cast<T1, T>(_column1.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T2)) return MemoryMarshal.Cast<T2, T>(_column2.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T3)) return MemoryMarshal.Cast<T3, T>(_column3.WriteAll(_view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _column1.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T2)) return _column2.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T3)) return _column3.HasChanged(index, _view.Ticks);
        throw ChunkChangeTicks.NotQueried<T>();
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2, out ReadOnlySpan<T3> components3)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2, out ReadOnlySpan<T3> components3, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        entities = Entities;
    }
}

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}(int)" /> a row, or
///     <see cref="WriteAll{T}" /> every row, to change them, so the write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2, T3, T4>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
    where T4 : unmanaged, IComponentData
{
    private readonly ChunkView _view;
    private readonly ChunkColumn<T1> _column1;
    private readonly ChunkColumn<T2> _column2;
    private readonly ChunkColumn<T3> _column3;
    private readonly ChunkColumn<T4> _column4;

    internal QueryChunk(ChunkView view, ComponentId id1, ComponentId id2, ComponentId id3, ComponentId id4)
    {
        _view = view;
        _column1 = view.Column<T1>(id1);
        _column2 = view.Column<T2>(id2);
        _column3 = view.Column<T3>(id3);
        _column4 = view.Column<T4>(id4);
    }

    public ReadOnlySpan<T1> Components1 => _column1.Components;

    public ReadOnlySpan<T2> Components2 => _column2.Components;

    public ReadOnlySpan<T3> Components3 => _column3.Components;

    public ReadOnlySpan<T4> Components4 => _column4.Components;

    public ChunkEntities Entities => _view.Entities;

    public int Length => _view.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _column1.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _column2.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T3)) return ref Unsafe.As<T3, T>(ref _column3.Write(index, _view.Ticks));
        if (typeof(T) == typeof(T4)) return ref Unsafe.As<T4, T>(ref _column4.Write(index, _view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Every row of <typeparamref name="T" />, one of the queried components, stamped as changed at once. Faster than
    ///     <see cref="Write{T}(int)" /> per row, but a change query then sees every row as changed.
    /// </summary>
    public Span<T> WriteAll<T>() where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return MemoryMarshal.Cast<T1, T>(_column1.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T2)) return MemoryMarshal.Cast<T2, T>(_column2.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T3)) return MemoryMarshal.Cast<T3, T>(_column3.WriteAll(_view.Ticks));
        if (typeof(T) == typeof(T4)) return MemoryMarshal.Cast<T4, T>(_column4.WriteAll(_view.Ticks));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _column1.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T2)) return _column2.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T3)) return _column3.HasChanged(index, _view.Ticks);
        if (typeof(T) == typeof(T4)) return _column4.HasChanged(index, _view.Ticks);
        throw ChunkChangeTicks.NotQueried<T>();
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2, out ReadOnlySpan<T3> components3, out ReadOnlySpan<T4> components4)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        components4 = Components4;
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ReadOnlySpan<T2> components2, out ReadOnlySpan<T3> components3, out ReadOnlySpan<T4> components4, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        components4 = Components4;
        entities = Entities;
    }
}
