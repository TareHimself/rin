using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The buffer lanes, texture lanes and shared lane, with the operations that need more than one of them.
///     Shared work holds the shared lock so fences land in the same order in every lane; whole-set operations
///     then take buffer and texture, in that order. A plain write takes only its own kind's lock.
/// </summary>
internal sealed class UploadLanes
{
    private readonly List<PendingResourceAction> _scratch = [];

    private readonly BufferWriteLanes _buffers = new();
    private readonly TextureWriteLanes _textures = new();
    private readonly SharedWriteLane _shared = new();

    public void EnqueueBuffer(in ResourceHandle handle, PendingBufferWrite write) => _buffers.Enqueue(handle, write);

    public void EnqueueTexture(in ResourceHandle handle, PendingTextureWrite write) => _textures.Enqueue(handle, write);

    public int Count(in ResourceHandle handle) => For(handle).Count(handle);

    public int Peek(in ResourceHandle handle, List<PendingResourceAction> into) => For(handle).Peek(handle, into);

    public void PeekShared(List<SharedWrite> into) => _shared.Peek(into);

    public void EnqueueShared(SharedWrite write)
    {
        lock (_shared.Sync)
        {
            _shared.Append(write);
            For(write.Source.Handle).EnqueueFence(write.Source.Handle, write);
            if (!write.IsSingleLane) For(write.Destination.Handle).EnqueueFence(write.Destination.Handle, write);
        }
    }

    public int Take(in ResourceHandle handle, int count, List<PendingResourceAction> into)
    {
        lock (_shared.Sync)
        {
            var start = into.Count;
            var taken = For(handle).Take(handle, count, into);
            for (var i = start; i < into.Count; i++)
                if (into[i] is SharedWrite shared)
                    _shared.MarkTaken(shared);
            return taken;
        }
    }

    /// <summary>
    ///     Takes shared writes in enqueue order, each with the writes ahead of it in its lanes, then whole write
    ///     segments round-robin, up to <paramref name="max" /> actions. The first shared write is always taken even
    ///     when it is bigger than the budget, so a large one cannot starve.
    /// </summary>
    public void TakeOrdered(int max, List<(ResourceHandle Handle, PendingResourceAction Action)> into)
    {
        lock (_shared.Sync)
        lock (_buffers.Sync)
        lock (_textures.Sync)
        {
            var budget = max;
            var start = into.Count;
            while (budget > 0 && TryTakeFirstShared(ref budget, into.Count == start, into)) { }

            bool progressed;
            do
            {
                progressed = budget > 0 && _buffers.TakeNextSegment(ref budget, into);
                progressed |= budget > 0 && _textures.TakeNextSegment(ref budget, into);
            } while (progressed && budget > 0);
        }
    }

    public void TakeAll(List<PendingResourceAction> into)
    {
        lock (_shared.Sync)
        lock (_buffers.Sync)
        lock (_textures.Sync)
        {
            _buffers.TakeAllWrites(into);
            _textures.TakeAllWrites(into);
            var shared = new List<SharedWrite>();
            _shared.TakeAll(shared);
            into.AddRange(shared);
        }
    }

    /// <summary>
    ///     Removes a resource's lane and cancels every shared write that names it, taking their fences out of
    ///     the other lane too.
    /// </summary>
    public void Drop(in ResourceHandle handle)
    {
        List<PendingResourceAction> discarded = [];
        lock (_shared.Sync)
        lock (_buffers.Sync)
        lock (_textures.Sync)
        {
            if (For(handle).DropLane(handle) is not { } items) return;

            foreach (var item in items)
            {
                if (item is not SharedWrite shared)
                {
                    discarded.Add(item);
                    continue;
                }

                if (shared.Taken) continue;
                _shared.MarkTaken(shared);
                RemoveOtherFence(shared, handle);
                discarded.Add(shared);
            }
        }

        foreach (var item in discarded) item.Dispose();
    }

    private bool TryTakeFirstShared(ref int budget, bool force, List<(ResourceHandle Handle, PendingResourceAction Action)> into)
    {
        if (_shared.First() is not { } shared) return false;

        var source = For(shared.Source.Handle);
        var destination = For(shared.Destination.Handle);
        if (!source.IsFirstFence(shared.Source.Handle, shared) ||
            !destination.IsFirstFence(shared.Destination.Handle, shared))
            return false;

        var sourceWrites = source.WritesBeforeFence(shared.Source.Handle);
        var destinationWrites = shared.IsSingleLane ? 0 : destination.WritesBeforeFence(shared.Destination.Handle);
        var cost = sourceWrites + destinationWrites + 1;
        if (cost > budget && !force) return false;

        TakeWrites(source, shared.Source.Handle, sourceWrites, into);
        TakeWrites(destination, shared.Destination.Handle, destinationWrites, into);
        into.Add((shared.Source.Handle, shared));

        _scratch.Clear();
        source.Take(shared.Source.Handle, 1, _scratch);
        if (!shared.IsSingleLane) destination.Take(shared.Destination.Handle, 1, _scratch);
        _scratch.Clear();
        _shared.MarkTaken(shared);
        budget -= cost;
        return true;
    }

    private void TakeWrites(IResourceLanes lanes, in ResourceHandle handle, int count,
        List<(ResourceHandle Handle, PendingResourceAction Action)> into)
    {
        if (count == 0) return;
        _scratch.Clear();
        lanes.Take(handle, count, _scratch);
        foreach (var write in _scratch) into.Add((handle, write));
        _scratch.Clear();
    }

    private void RemoveOtherFence(SharedWrite shared, in ResourceHandle dropped)
    {
        if (!shared.Source.Handle.Equals(dropped)) For(shared.Source.Handle).RemoveFence(shared.Source.Handle, shared);
        if (!shared.Destination.Handle.Equals(dropped))
            For(shared.Destination.Handle).RemoveFence(shared.Destination.Handle, shared);
    }

    private IResourceLanes For(in ResourceHandle handle)
    {
        return handle.Type == ResourceType.Buffer ? _buffers : _textures;
    }
}
