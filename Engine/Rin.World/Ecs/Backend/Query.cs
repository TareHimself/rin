using Friflo.Engine.ECS;

namespace Rin.World.Ecs;

public readonly struct Query<T1>
    where T1 : struct, IComponentData
{
    private readonly ArchetypeQuery<T1> _query;

    internal Query(ArchetypeQuery<T1> query) => _query = query;

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1> GetEnumerator() => new(_query.Chunks.GetEnumerator());
}

public struct QueryEnumerator<T1> : IDisposable
    where T1 : struct, IComponentData
{
    private ChunkEnumerator<T1> _chunks;

    internal QueryEnumerator(ChunkEnumerator<T1> chunks) => _chunks = chunks;

    public QueryChunk<T1> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1>(chunks.Chunk1.Span, new ChunkEntities(chunks.Entities));
        }
    }

    public bool MoveNext() => _chunks.MoveNext();

    // Ends the backend's structural change guard for this loop.
    public void Dispose() => _chunks.Dispose();
}

public readonly struct Query<T1, T2>
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
{
    private readonly ArchetypeQuery<T1, T2> _query;

    internal Query(ArchetypeQuery<T1, T2> query) => _query = query;

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2> GetEnumerator() => new(_query.Chunks.GetEnumerator());
}

public struct QueryEnumerator<T1, T2> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2> _chunks;

    internal QueryEnumerator(ChunkEnumerator<T1, T2> chunks) => _chunks = chunks;

    public QueryChunk<T1, T2> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2>(chunks.Chunk1.Span, chunks.Chunk2.Span, new ChunkEntities(chunks.Entities));
        }
    }

    public bool MoveNext() => _chunks.MoveNext();

    // Ends the backend's structural change guard for this loop.
    public void Dispose() => _chunks.Dispose();
}

public readonly struct Query<T1, T2, T3>
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
{
    private readonly ArchetypeQuery<T1, T2, T3> _query;

    internal Query(ArchetypeQuery<T1, T2, T3> query) => _query = query;

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2, T3> GetEnumerator() => new(_query.Chunks.GetEnumerator());
}

public struct QueryEnumerator<T1, T2, T3> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2, T3> _chunks;

    internal QueryEnumerator(ChunkEnumerator<T1, T2, T3> chunks) => _chunks = chunks;

    public QueryChunk<T1, T2, T3> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2, T3>(chunks.Chunk1.Span, chunks.Chunk2.Span, chunks.Chunk3.Span, new ChunkEntities(chunks.Entities));
        }
    }

    public bool MoveNext() => _chunks.MoveNext();

    // Ends the backend's structural change guard for this loop.
    public void Dispose() => _chunks.Dispose();
}

public readonly struct Query<T1, T2, T3, T4>
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
{
    private readonly ArchetypeQuery<T1, T2, T3, T4> _query;

    internal Query(ArchetypeQuery<T1, T2, T3, T4> query) => _query = query;

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator() => new(_query.Chunks.GetEnumerator());
}

public struct QueryEnumerator<T1, T2, T3, T4> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2, T3, T4> _chunks;

    internal QueryEnumerator(ChunkEnumerator<T1, T2, T3, T4> chunks) => _chunks = chunks;

    public QueryChunk<T1, T2, T3, T4> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2, T3, T4>(chunks.Chunk1.Span, chunks.Chunk2.Span, chunks.Chunk3.Span, chunks.Chunk4.Span, new ChunkEntities(chunks.Entities));
        }
    }

    public bool MoveNext() => _chunks.MoveNext();

    // Ends the backend's structural change guard for this loop.
    public void Dispose() => _chunks.Dispose();
}
