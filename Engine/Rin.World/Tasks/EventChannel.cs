using System.Runtime.InteropServices;

namespace Rin.World.Tasks;

internal abstract class EventChannel
{
    private static int _slotCount;

    /// <summary>
    ///     Drops the events raised before <paramref name="cutoff" />, which every wait has had its chance to see.
    /// </summary>
    public abstract void TrimBefore(long cutoff);

    protected static int NextSlot() => Interlocked.Increment(ref _slotCount) - 1;
}

/// <summary>
///     The events of one type raised since the last Tasks phase, in raise order.
/// </summary>
internal sealed class EventChannel<T> : EventChannel where T : unmanaged
{
    public static readonly int Slot = NextSlot();

    private readonly List<RaisedEvent> _raised = [];

    public void Raise(Entity target, in T value, long sequence) => _raised.Add(new RaisedEvent(target, value, sequence));

    /// <summary>
    ///     The first event for <paramref name="target" /> raised after <paramref name="after" /> and before
    ///     <paramref name="before" />.
    /// </summary>
    public bool TryFind(Entity target, long after, long before, out T value)
    {
        foreach (ref readonly var raised in CollectionsMarshal.AsSpan(_raised))
        {
            if (raised.Sequence >= before) break;
            if (raised.Sequence <= after || raised.Target != target) continue;
            value = raised.Value;
            return true;
        }

        value = default;
        return false;
    }

    public override void TrimBefore(long cutoff)
    {
        var count = 0;
        while (count < _raised.Count && _raised[count].Sequence < cutoff) count++;
        _raised.RemoveRange(0, count);
    }

    private readonly record struct RaisedEvent(Entity Target, T Value, long Sequence);
}
