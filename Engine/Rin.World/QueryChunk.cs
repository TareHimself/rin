using System.Runtime.CompilerServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}" /> a row to change it, so the
///     write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1>
    where T1 : unmanaged, IComponentData
{
    private readonly Span<T1> _components1;
    private readonly Column _column1;
    private readonly ChunkChangeTicks _ticks;

    internal QueryChunk(Column column1, ChunkEntities entities, ChunkChangeTicks ticks)
    {
        _column1 = column1;
        _components1 = column1.Span<T1>(entities.Length);
        _ticks = ticks;
        Entities = entities;
    }

    public ReadOnlySpan<T1> Components1 => _components1;

    public ChunkEntities Entities { get; }

    public int Length => Entities.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _ticks.Stamp(_components1, _column1, index));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _ticks.IsNewer(_column1, index);
        throw ChunkChangeTicks.NotQueried<T>();
    }

    public void Deconstruct(out ReadOnlySpan<T1> components1, out ChunkEntities entities)
    {
        components1 = Components1;
        entities = Entities;
    }
}

/// <summary>
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}" /> a row to change it, so the
///     write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
{
    private readonly Span<T1> _components1;
    private readonly Span<T2> _components2;
    private readonly Column _column1;
    private readonly Column _column2;
    private readonly ChunkChangeTicks _ticks;

    internal QueryChunk(Column column1, Column column2, ChunkEntities entities, ChunkChangeTicks ticks)
    {
        _column1 = column1;
        _components1 = column1.Span<T1>(entities.Length);
        _column2 = column2;
        _components2 = column2.Span<T2>(entities.Length);
        _ticks = ticks;
        Entities = entities;
    }

    public ReadOnlySpan<T1> Components1 => _components1;

    public ReadOnlySpan<T2> Components2 => _components2;

    public ChunkEntities Entities { get; }

    public int Length => Entities.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _ticks.Stamp(_components1, _column1, index));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _ticks.Stamp(_components2, _column2, index));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _ticks.IsNewer(_column1, index);
        if (typeof(T) == typeof(T2)) return _ticks.IsNewer(_column2, index);
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
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}" /> a row to change it, so the
///     write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2, T3>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
{
    private readonly Span<T1> _components1;
    private readonly Span<T2> _components2;
    private readonly Span<T3> _components3;
    private readonly Column _column1;
    private readonly Column _column2;
    private readonly Column _column3;
    private readonly ChunkChangeTicks _ticks;

    internal QueryChunk(Column column1, Column column2, Column column3, ChunkEntities entities, ChunkChangeTicks ticks)
    {
        _column1 = column1;
        _components1 = column1.Span<T1>(entities.Length);
        _column2 = column2;
        _components2 = column2.Span<T2>(entities.Length);
        _column3 = column3;
        _components3 = column3.Span<T3>(entities.Length);
        _ticks = ticks;
        Entities = entities;
    }

    public ReadOnlySpan<T1> Components1 => _components1;

    public ReadOnlySpan<T2> Components2 => _components2;

    public ReadOnlySpan<T3> Components3 => _components3;

    public ChunkEntities Entities { get; }

    public int Length => Entities.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _ticks.Stamp(_components1, _column1, index));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _ticks.Stamp(_components2, _column2, index));
        if (typeof(T) == typeof(T3)) return ref Unsafe.As<T3, T>(ref _ticks.Stamp(_components3, _column3, index));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _ticks.IsNewer(_column1, index);
        if (typeof(T) == typeof(T2)) return _ticks.IsNewer(_column2, index);
        if (typeof(T) == typeof(T3)) return _ticks.IsNewer(_column3, index);
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
///     One archetype's rows of a query. The spans are read-only: <see cref="Write{T}" /> a row to change it, so the
///     write is stamped with the change tick.
/// </summary>
public readonly ref struct QueryChunk<T1, T2, T3, T4>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
    where T4 : unmanaged, IComponentData
{
    private readonly Span<T1> _components1;
    private readonly Span<T2> _components2;
    private readonly Span<T3> _components3;
    private readonly Span<T4> _components4;
    private readonly Column _column1;
    private readonly Column _column2;
    private readonly Column _column3;
    private readonly Column _column4;
    private readonly ChunkChangeTicks _ticks;

    internal QueryChunk(Column column1, Column column2, Column column3, Column column4, ChunkEntities entities, ChunkChangeTicks ticks)
    {
        _column1 = column1;
        _components1 = column1.Span<T1>(entities.Length);
        _column2 = column2;
        _components2 = column2.Span<T2>(entities.Length);
        _column3 = column3;
        _components3 = column3.Span<T3>(entities.Length);
        _column4 = column4;
        _components4 = column4.Span<T4>(entities.Length);
        _ticks = ticks;
        Entities = entities;
    }

    public ReadOnlySpan<T1> Components1 => _components1;

    public ReadOnlySpan<T2> Components2 => _components2;

    public ReadOnlySpan<T3> Components3 => _components3;

    public ReadOnlySpan<T4> Components4 => _components4;

    public ChunkEntities Entities { get; }

    public int Length => Entities.Length;

    /// <summary>
    ///     Row <paramref name="index" /> of <typeparamref name="T" />, one of the queried components, stamped as changed.
    /// </summary>
    public ref T Write<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return ref Unsafe.As<T1, T>(ref _ticks.Stamp(_components1, _column1, index));
        if (typeof(T) == typeof(T2)) return ref Unsafe.As<T2, T>(ref _ticks.Stamp(_components2, _column2, index));
        if (typeof(T) == typeof(T3)) return ref Unsafe.As<T3, T>(ref _ticks.Stamp(_components3, _column3, index));
        if (typeof(T) == typeof(T4)) return ref Unsafe.As<T4, T>(ref _ticks.Stamp(_components4, _column4, index));
        throw ChunkChangeTicks.NotQueried<T>();
    }

    /// <summary>
    ///     Whether row <paramref name="index" /> of <typeparamref name="T" /> was written since the query's last run.
    ///     Only for a query built with <c>Changed</c>.
    /// </summary>
    public bool HasChanged<T>(int index) where T : unmanaged, IComponentData
    {
        if (typeof(T) == typeof(T1)) return _ticks.IsNewer(_column1, index);
        if (typeof(T) == typeof(T2)) return _ticks.IsNewer(_column2, index);
        if (typeof(T) == typeof(T3)) return _ticks.IsNewer(_column3, index);
        if (typeof(T) == typeof(T4)) return _ticks.IsNewer(_column4, index);
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
