namespace Rin.World;

public readonly ref struct QueryChunk<T1>(Span<T1> components1, ChunkEntities entities)
    where T1 : struct, IComponentData
{
    public Span<T1> Components1 { get; } = components1;

    public ChunkEntities Entities { get; } = entities;

    public int Length => Entities.Length;

    public void Deconstruct(out Span<T1> components1, out ChunkEntities entities)
    {
        components1 = Components1;
        entities = Entities;
    }
}

public readonly ref struct QueryChunk<T1, T2>(Span<T1> components1, Span<T2> components2, ChunkEntities entities)
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
{
    public Span<T1> Components1 { get; } = components1;

    public Span<T2> Components2 { get; } = components2;

    public ChunkEntities Entities { get; } = entities;

    public int Length => Entities.Length;

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2)
    {
        components1 = Components1;
        components2 = Components2;
    }

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        entities = Entities;
    }
}

public readonly ref struct QueryChunk<T1, T2, T3>(Span<T1> components1, Span<T2> components2, Span<T3> components3, ChunkEntities entities)
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
{
    public Span<T1> Components1 { get; } = components1;

    public Span<T2> Components2 { get; } = components2;

    public Span<T3> Components3 { get; } = components3;

    public ChunkEntities Entities { get; } = entities;

    public int Length => Entities.Length;

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2, out Span<T3> components3)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
    }

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2, out Span<T3> components3, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        entities = Entities;
    }
}

public readonly ref struct QueryChunk<T1, T2, T3, T4>(Span<T1> components1, Span<T2> components2, Span<T3> components3, Span<T4> components4, ChunkEntities entities)
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
{
    public Span<T1> Components1 { get; } = components1;

    public Span<T2> Components2 { get; } = components2;

    public Span<T3> Components3 { get; } = components3;

    public Span<T4> Components4 { get; } = components4;

    public ChunkEntities Entities { get; } = entities;

    public int Length => Entities.Length;

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2, out Span<T3> components3, out Span<T4> components4)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        components4 = Components4;
    }

    public void Deconstruct(out Span<T1> components1, out Span<T2> components2, out Span<T3> components3, out Span<T4> components4, out ChunkEntities entities)
    {
        components1 = Components1;
        components2 = Components2;
        components3 = Components3;
        components4 = Components4;
        entities = Entities;
    }
}
