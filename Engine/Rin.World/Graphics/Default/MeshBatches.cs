using Rin.Core.Shared;

namespace Rin.World.Graphics.Default;

/// <summary>
///     A frame's surfaces grouped into batches that draw together, in the order each batch first appears.
///     A batch is a run of indices into the frame's processed meshes.
/// </summary>
public sealed class MeshBatches : IDisposable
{
    private PooledMemory<int> _order;
    private PooledMemory<int> _starts;
    private bool _disposed;

    private MeshBatches(int count, PooledMemory<int> order, PooledMemory<int> starts)
    {
        Count = count;
        _order = order;
        _starts = starts;
    }

    public int Count { get; }

    public ReadOnlySpan<int> this[int batch] => _order.AsSpan()[_starts[batch].._starts[batch + 1]];

    public static MeshBatches Group(ReadOnlySpan<ProcessedMesh> meshes,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        Dictionary<DefaultSceneFrame.BatchKey, int> batchIds = [];
        using var batchOfMesh = new PooledMemory<int>(meshes.Length);
        // A batch holds at least one mesh, so there are never more batches than meshes.
        using var sizes = new PooledMemory<int>(meshes.Length).Clear();
        for (var i = 0; i < meshes.Length; i++)
        {
            var key = new DefaultSceneFrame.BatchKey(meshes[i].IndexBuffer,
                MaterialIdentity.Of(selectPass(meshes[i].Material)));
            if (!batchIds.TryGetValue(key, out var batch)) batchIds.Add(key, batch = batchIds.Count);
            batchOfMesh[i] = batch;
            sizes[batch]++;
        }

        var count = batchIds.Count;
        var starts = new PooledMemory<int>(count + 1);
        starts[0] = 0;
        for (var batch = 0; batch < count; batch++) starts[batch + 1] = starts[batch] + sizes[batch];

        var order = new PooledMemory<int>(meshes.Length);
        var filled = sizes.Clear();
        for (var i = 0; i < meshes.Length; i++)
        {
            var batch = batchOfMesh[i];
            order[starts[batch] + filled[batch]++] = i;
        }

        return new MeshBatches(count, order, starts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _order.Dispose();
        _starts.Dispose();
    }
}
