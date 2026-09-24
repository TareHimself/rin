using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

internal abstract class ResourceLanes<TAction> where TAction : class, IDisposable
{
    private readonly Dictionary<ResourceHandle, List<TAction>> _lanes = [];
    private readonly Queue<ResourceHandle> _rotation = [];
    private readonly HashSet<ResourceHandle> _inRotation = [];
    private readonly Lock _sync = new();

    public void Enqueue(in ResourceHandle handle, TAction action)
    {
        List<TAction>? discarded = null;
        lock (_sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane)) _lanes[handle] = lane = [];
            if (_inRotation.Add(handle)) _rotation.Enqueue(handle);

            var result = action;
            while (lane.Count > 0 && CanMerge(lane[^1], result))
            {
                var existing = lane[^1];
                lane.RemoveAt(lane.Count - 1);
                var merged = Merge(existing, result);
                if (!ReferenceEquals(merged, existing)) (discarded ??= []).Add(existing);
                if (!ReferenceEquals(merged, result)) (discarded ??= []).Add(result);
                result = merged;
            }

            lane.Add(result);
        }

        if (discarded is not null)
            foreach (var item in discarded)
                item.Dispose();
    }

    public int Peek(in ResourceHandle handle, out TAction? first)
    {
        lock (_sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane))
            {
                first = null;
                return 0;
            }

            first = lane[0];
            return lane.Count;
        }
    }

    public int TryTake(in ResourceHandle handle, int max, List<TAction> into)
    {
        lock (_sync)
        {
            if (!_lanes.TryGetValue(handle, out var lane)) return 0;

            var count = Math.Min(max, lane.Count);
            into.AddRange(lane.GetRange(0, count));
            lane.RemoveRange(0, count);
            if (lane.Count == 0) _lanes.Remove(handle);
            return count;
        }
    }

    public void TakeAll(List<(ResourceHandle Handle, TAction Action)> into)
    {
        TakeUpTo(int.MaxValue, into);
    }

    public void TakeUpTo(int max, List<(ResourceHandle Handle, TAction Action)> into)
    {
        lock (_sync)
        {
            var remaining = max;
            while (remaining > 0 && _rotation.TryDequeue(out var handle))
            {
                _inRotation.Remove(handle);
                if (!_lanes.TryGetValue(handle, out var lane)) continue;

                into.Add((handle, lane[0]));
                lane.RemoveAt(0);
                remaining--;

                if (lane.Count == 0)
                    _lanes.Remove(handle);
                else if (_inRotation.Add(handle))
                    _rotation.Enqueue(handle);
            }
        }
    }

    public void Drop(in ResourceHandle handle)
    {
        List<TAction>? lane;
        lock (_sync)
        {
            _lanes.Remove(handle, out lane);
        }

        if (lane is null) return;
        foreach (var action in lane) action.Dispose();
    }

    protected virtual bool CanMerge(TAction existing, TAction incoming)
    {
        return false;
    }

    protected virtual TAction Merge(TAction existing, TAction incoming)
    {
        throw new NotSupportedException();
    }
}
