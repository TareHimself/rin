using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The kind-independent face of a lane set, so shared work can reach the lanes it names.
/// </summary>
internal interface IResourceLanes
{
    Lock Sync { get; }
    int Count(in ResourceHandle handle);

    /// <summary>
    ///     Copies a lane's items without removing them and freezes them against merging, so a later
    ///     <see cref="Take" /> of that many items gets exactly these.
    /// </summary>
    int Peek(in ResourceHandle handle, List<PendingResourceAction> into);

    int Take(in ResourceHandle handle, int count, List<PendingResourceAction> into);
    void EnqueueFence(in ResourceHandle handle, SharedWrite fence);
    void RemoveFence(in ResourceHandle handle, SharedWrite fence);
    int WritesBeforeFence(in ResourceHandle handle);
    bool IsFirstFence(in ResourceHandle handle, SharedWrite fence);

    /// <summary>
    ///     Takes the writes before the next fence of the next unblocked lane in rotation, at most
    ///     <paramref name="budget" /> of them. False when no lane had any.
    /// </summary>
    bool TakeNextSegment(ref int budget, List<(ResourceHandle Handle, PendingResourceAction Action)> into);

    List<PendingResourceAction>? DropLane(in ResourceHandle handle);
    void TakeAllWrites(List<PendingResourceAction> into);
}

/// <summary>
///     Queued writes of one kind, one ordered lane per <see cref="ResourceHandle" />. A lane also holds the
///     <see cref="SharedWrite" /> fences that order it against other lanes, and merging never crosses one.
/// </summary>
internal abstract class ResourceLanes<TWrite> : IResourceLanes where TWrite : PendingWrite
{
    private readonly Dictionary<ResourceHandle, Lane> _lanes = [];
    private readonly Queue<ResourceHandle> _rotation = [];
    private readonly HashSet<ResourceHandle> _inRotation = [];

    public Lock Sync { get; } = new();

    public void Enqueue(in ResourceHandle handle, TWrite write)
    {
        List<PendingResourceAction>? discarded = null;
        lock (Sync)
        {
            var lane = GetOrAddLane(handle);
            while (lane.Items.Count > lane.Frozen && CanMerge(lane.Items[^1], write))
            {
                var existing = lane.Items[^1];
                lane.Items.RemoveAt(lane.Items.Count - 1);
                write.AdoptTasks(existing);
                (discarded ??= []).Add(existing);
            }

            lane.Items.Add(write);
        }

        if (discarded is null) return;
        foreach (var item in discarded) item.Dispose();
    }

    public void EnqueueFence(in ResourceHandle handle, SharedWrite fence)
    {
        lock (Sync)
        {
            GetOrAddLane(handle).Items.Add(fence);
        }
    }

    public int Count(in ResourceHandle handle)
    {
        lock (Sync)
        {
            return _lanes.TryGetValue(handle, out var lane) ? lane.Items.Count : 0;
        }
    }

    public int Peek(in ResourceHandle handle, List<PendingResourceAction> into)
    {
        lock (Sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane)) return 0;
            into.AddRange(lane.Items);
            lane.Frozen = lane.Items.Count;
            return lane.Items.Count;
        }
    }

    public int Take(in ResourceHandle handle, int count, List<PendingResourceAction> into)
    {
        lock (Sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane)) return 0;

            var taken = Math.Min(count, lane.Items.Count);
            for (var i = 0; i < taken; i++) into.Add(lane.Items[i]);
            lane.RemoveFromFront(taken);
            RemoveIfEmpty(handle, lane);
            return taken;
        }
    }

    public int WritesBeforeFence(in ResourceHandle handle)
    {
        lock (Sync)
        {
            return _lanes.TryGetValue(handle, out var lane) ? lane.WritesBeforeFence() : 0;
        }
    }

    public bool IsFirstFence(in ResourceHandle handle, SharedWrite fence)
    {
        lock (Sync)
        {
            return _lanes.TryGetValue(handle, out var lane) && lane.IsFirstFence(fence);
        }
    }

    public void RemoveFence(in ResourceHandle handle, SharedWrite fence)
    {
        lock (Sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane)) return;
            lane.RemoveItem(fence);
            RemoveIfEmpty(handle, lane);
        }
    }

    public bool TakeNextSegment(ref int budget, List<(ResourceHandle Handle, PendingResourceAction Action)> into)
    {
        lock (Sync)
        {
            for (var attempts = _rotation.Count; attempts > 0; attempts--)
            {
                var handle = _rotation.Dequeue();
                _inRotation.Remove(handle);
                if (!_lanes.TryGetValue(handle, out var lane)) continue;

                var writes = Math.Min(lane.WritesBeforeFence(), budget);
                if (writes > 0)
                {
                    for (var i = 0; i < writes; i++) into.Add((handle, lane.Items[i]));
                    lane.RemoveFromFront(writes);
                    budget -= writes;
                    if (RemoveIfEmpty(handle, lane)) return true;
                }

                if (_inRotation.Add(handle)) _rotation.Enqueue(handle);
                if (writes > 0) return true;
            }

            return false;
        }
    }

    public List<PendingResourceAction>? DropLane(in ResourceHandle handle)
    {
        lock (Sync)
        {
            return _lanes.Remove(handle, out var lane) ? lane.Items : null;
        }
    }

    public void TakeAllWrites(List<PendingResourceAction> into)
    {
        lock (Sync)
        {
            foreach (var lane in _lanes.Values)
                foreach (var item in lane.Items)
                    if (item is not SharedWrite)
                        into.Add(item);

            _lanes.Clear();
            _rotation.Clear();
            _inRotation.Clear();
        }
    }

    protected abstract bool CanMerge(TWrite existing, TWrite incoming);

    private bool CanMerge(PendingResourceAction existing, TWrite incoming)
    {
        return existing is TWrite write && CanMerge(write, incoming);
    }

    private Lane GetOrAddLane(in ResourceHandle handle)
    {
        if (!_lanes.TryGetValue(handle, out var lane)) _lanes[handle] = lane = new Lane();
        if (_inRotation.Add(handle)) _rotation.Enqueue(handle);
        return lane;
    }

    private bool RemoveIfEmpty(in ResourceHandle handle, Lane lane)
    {
        if (lane.Items.Count > 0) return false;
        _lanes.Remove(handle);
        return true;
    }

    private sealed class Lane
    {
        public List<PendingResourceAction> Items { get; } = [];

        public int Frozen { get; set; }

        public int WritesBeforeFence()
        {
            var count = 0;
            while (count < Items.Count && Items[count] is not SharedWrite) count++;
            return count;
        }

        public bool IsFirstFence(SharedWrite fence)
        {
            var writes = WritesBeforeFence();
            return writes < Items.Count && ReferenceEquals(Items[writes], fence);
        }

        public void RemoveFromFront(int count)
        {
            Items.RemoveRange(0, count);
            Frozen = Math.Max(0, Frozen - count);
        }

        public void RemoveItem(PendingResourceAction item)
        {
            var index = Items.IndexOf(item);
            if (index < 0) return;
            Items.RemoveAt(index);
            if (index < Frozen) Frozen--;
        }
    }
}
