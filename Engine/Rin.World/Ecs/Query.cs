using Friflo.Engine.ECS;
using Rin.World.Ecs;

namespace Rin.World;

public readonly struct Query<T1>
    where T1 : struct, IComponentData
{
    private readonly ArchetypeQuery<T1> _query;
    private readonly EntityIdAllocator _ids;

    internal Query(ArchetypeQuery<T1> query, EntityIdAllocator ids)
    {
        _query = query;
        _ids = ids;
    }

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1> GetEnumerator() => new(_query.Chunks.GetEnumerator(), _ids);
}

public struct QueryEnumerator<T1> : IDisposable
    where T1 : struct, IComponentData
{
    private ChunkEnumerator<T1> _chunks;
    private readonly EntityIdAllocator _ids;

    internal QueryEnumerator(ChunkEnumerator<T1> chunks, EntityIdAllocator ids)
    {
        _chunks = chunks;
        _ids = ids;
    }

    public QueryChunk<T1> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1>(chunks.Chunk1.Span, new ChunkEntities(chunks.Entities, _ids));
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
    private readonly EntityIdAllocator _ids;

    internal Query(ArchetypeQuery<T1, T2> query, EntityIdAllocator ids)
    {
        _query = query;
        _ids = ids;
    }

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2> GetEnumerator() => new(_query.Chunks.GetEnumerator(), _ids);
}

public struct QueryEnumerator<T1, T2> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2> _chunks;
    private readonly EntityIdAllocator _ids;

    internal QueryEnumerator(ChunkEnumerator<T1, T2> chunks, EntityIdAllocator ids)
    {
        _chunks = chunks;
        _ids = ids;
    }

    public QueryChunk<T1, T2> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2>(chunks.Chunk1.Span, chunks.Chunk2.Span, new ChunkEntities(chunks.Entities, _ids));
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
    private readonly EntityIdAllocator _ids;

    internal Query(ArchetypeQuery<T1, T2, T3> query, EntityIdAllocator ids)
    {
        _query = query;
        _ids = ids;
    }

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2, T3> GetEnumerator() => new(_query.Chunks.GetEnumerator(), _ids);
}

public struct QueryEnumerator<T1, T2, T3> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2, T3> _chunks;
    private readonly EntityIdAllocator _ids;

    internal QueryEnumerator(ChunkEnumerator<T1, T2, T3> chunks, EntityIdAllocator ids)
    {
        _chunks = chunks;
        _ids = ids;
    }

    public QueryChunk<T1, T2, T3> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2, T3>(chunks.Chunk1.Span, chunks.Chunk2.Span, chunks.Chunk3.Span, new ChunkEntities(chunks.Entities, _ids));
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
    private readonly EntityIdAllocator _ids;

    internal Query(ArchetypeQuery<T1, T2, T3, T4> query, EntityIdAllocator ids)
    {
        _query = query;
        _ids = ids;
    }

    public int EntityCount => _query.Count;

    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator() => new(_query.Chunks.GetEnumerator(), _ids);
}

public struct QueryEnumerator<T1, T2, T3, T4> : IDisposable
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
{
    private ChunkEnumerator<T1, T2, T3, T4> _chunks;
    private readonly EntityIdAllocator _ids;

    internal QueryEnumerator(ChunkEnumerator<T1, T2, T3, T4> chunks, EntityIdAllocator ids)
    {
        _chunks = chunks;
        _ids = ids;
    }

    public QueryChunk<T1, T2, T3, T4> Current
    {
        get
        {
            var chunks = _chunks.Current;
            return new QueryChunk<T1, T2, T3, T4>(chunks.Chunk1.Span, chunks.Chunk2.Span, chunks.Chunk3.Span, chunks.Chunk4.Span, new ChunkEntities(chunks.Entities, _ids));
        }
    }

    public bool MoveNext() => _chunks.MoveNext();

    // Ends the backend's structural change guard for this loop.
    public void Dispose() => _chunks.Dispose();
}
