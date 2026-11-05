using System.Runtime.ExceptionServices;
using Rin.World.Scheduling;
using Rin.World.Scripting;

namespace Rin.World.Tasks;

/// <summary>
///     Runs <see cref="GameTask" />s on one thread. Waits are resumed only in <see cref="TickPhase.Tasks" />, in the
///     order they were queued, and a wait whose owner entity is gone is cancelled instead of resumed.
/// </summary>
/// <remarks>
///     A cancelled task stops silently: the awaiting method is never resumed, so its <c>catch</c> and <c>finally</c>
///     blocks do not run. An exception that escapes a task started with <see cref="Run" /> is reported at the end of the
///     Tasks phase to <see cref="TaskFaulted" />, or rethrown from it when nothing subscribed. A snapshot restore cancels
///     every task and drops every raised event that was not delivered yet.
/// </remarks>
public sealed class GameTaskScheduler : IWorldRestoreListener
{
    private readonly List<WaitEntry> _waiting = [];
    private readonly List<WaitEntry> _due = [];
    private readonly List<EventChannel> _channels = [];
    private readonly List<TaskFault> _faults = [];
    private EventChannel?[] _channelsBySlot = [];
    private long _sequence;

    public GameTaskScheduler(World world)
    {
        World = world;
        world.AddSystem(TickPhase.Tasks, new TaskResume(this));
        world.AddRestoreListener(this);
    }

    public World World { get; }

    /// <summary>
    ///     Tasks started with <see cref="Run" /> that have not completed or been cancelled.
    /// </summary>
    public int RunningCount { get; private set; }

    /// <summary>
    ///     Receives the owner and the exception of every task started with <see cref="Run" /> that faulted.
    /// </summary>
    public event Action<Entity, Exception>? TaskFaulted;

    /// <summary>
    ///     How many Tasks phases have run. A tick wait for <c>n</c> ticks resumes in phase <c>Phase + n</c>.
    /// </summary>
    internal long Phase { get; private set; }

    /// <summary>
    ///     Events raised before this sequence are delivered in the current phase, later ones in the next.
    /// </summary>
    internal long EventCutoff { get; private set; }

    /// <summary>
    ///     Calls <paramref name="body" /> now and keeps the task it returns running. It is cancelled when
    ///     <paramref name="owner" /> is despawned, unless the owner is <c>default</c>.
    /// </summary>
    public void Run(Entity owner, Func<TaskContext, GameTask> body) => Run(owner, null, body);

    /// <summary>
    ///     Completes the <see cref="TaskContext.Event{T}" /> waits for <paramref name="target" /> in the next Tasks phase,
    ///     or in the current one when it has not started resuming yet. An event nobody waits for is dropped.
    /// </summary>
    public void Raise<T>(Entity target, in T value) where T : unmanaged => Channel<T>().Raise(target, value, _sequence++);

    /// <summary>
    ///     Cancels every task waiting on something owned by <paramref name="owner" />.
    /// </summary>
    public void Cancel(Entity owner) => CancelWaits(new CancelFilter(owner, null, false));

    public void CancelAll() => CancelWaits(new CancelFilter(default, null, true));

    void IWorldRestoreListener.BeforeRestore()
    {
        CancelAll();
        for (var i = 0; i < _channels.Count; i++) _channels[i].Clear();
    }

    void IWorldRestoreListener.AfterRestore()
    {
    }

    internal void Run(Entity owner, Script? script, Func<TaskContext, GameTask> body)
    {
        if (body(new TaskContext(this, owner, script)).TakeNode() is not { } node) return;

        node.MarkRoot(this);
        node.Owner = owner;
        if (node.Status == TaskNodeStatus.Pending) RunningCount++;
        else FinishRoot(node);
    }

    internal void Cancel(Script script) => CancelWaits(new CancelFilter(default, script, false));

    internal GameTask Enqueue(TaskNode wait, in TaskContext context)
    {
        wait.Owner = context.Owner;
        wait.Script = context.Script;
        wait.Sequence = _sequence++;
        _waiting.Add(new WaitEntry(wait));
        return new GameTask(wait);
    }

    internal GameTask<T> Enqueue<T>(TaskNode<T> wait, in TaskContext context)
    {
        Enqueue((TaskNode)wait, context);
        return new GameTask<T>(wait);
    }

    internal EventChannel<T> Channel<T>() where T : unmanaged
    {
        var slot = EventChannel<T>.Slot;
        if (slot >= _channelsBySlot.Length) Array.Resize(ref _channelsBySlot, slot + 1);
        if (_channelsBySlot[slot] is EventChannel<T> channel) return channel;

        channel = new EventChannel<T>();
        _channelsBySlot[slot] = channel;
        _channels.Add(channel);
        return channel;
    }

    internal void ResumeDue()
    {
        Phase++;
        EventCutoff = _sequence;
        try
        {
            CollectDue();
            ResumeCollected();
        }
        finally
        {
            _due.Clear();
            for (var i = 0; i < _channels.Count; i++) _channels[i].TrimBefore(EventCutoff);
        }

        ReportFaults();
    }

    internal void OnRootCompleted(TaskNode root)
    {
        RunningCount--;
        FinishRoot(root);
    }

    private void CollectDue()
    {
        // Until conditions are user code and may queue more waits; those stay after count for the next phase.
        var count = _waiting.Count;
        var kept = 0;
        for (var i = 0; i < count; i++)
        {
            var entry = _waiting[i];
            if (!entry.IsPending) continue;
            if (!IsOwnerAlive(entry.Node)) entry.Node.Cancel();
            else if (entry.Node.IsDue(this)) _due.Add(entry);
            else _waiting[kept++] = entry;
        }

        _waiting.RemoveRange(kept, count - kept);
    }

    private void ResumeCollected()
    {
        for (var i = 0; i < _due.Count; i++)
        {
            var entry = _due[i];
            if (!entry.IsPending) continue;
            if (IsOwnerAlive(entry.Node)) entry.Node.CompleteDue();
            else entry.Node.Cancel();
        }
    }

    private bool IsOwnerAlive(TaskNode wait) => !wait.Owner.IsValid || World.IsAlive(wait.Owner);

    private void CancelWaits(in CancelFilter filter)
    {
        CancelWaits(_waiting, filter);
        CancelWaits(_due, filter);
    }

    private static void CancelWaits(List<WaitEntry> entries, in CancelFilter filter)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.IsPending && filter.Matches(entry.Node)) entry.Node.Cancel();
        }
    }

    private void FinishRoot(TaskNode root)
    {
        if (root.Status == TaskNodeStatus.Faulted) _faults.Add(new TaskFault(root.Owner, root.TakeError()));
        root.Release();
    }

    private void ReportFaults()
    {
        if (_faults.Count == 0) return;

        if (TaskFaulted is { } handler)
        {
            for (var i = 0; i < _faults.Count; i++) handler(_faults[i].Owner, _faults[i].Error.SourceException);
            _faults.Clear();
            return;
        }

        var first = _faults[0].Error;
        var all = _faults.Count == 1 ? null : _faults.ConvertAll(static fault => fault.Error.SourceException);
        _faults.Clear();
        if (all is not null) throw new AggregateException("Several GameTasks faulted in one Tasks phase.", all);
        first.Throw();
    }

    /// <summary>
    ///     A queued wait. The version tells a stale entry, for a node cancelled and released since, from a live one.
    /// </summary>
    private readonly struct WaitEntry(TaskNode node)
    {
        private readonly short _version = node.Version;

        public TaskNode Node { get; } = node;

        public bool IsPending => Node.IsPending(_version);
    }

    private readonly record struct TaskFault(Entity Owner, ExceptionDispatchInfo Error);

    private readonly struct CancelFilter(Entity owner, Script? script, bool all)
    {
        public bool Matches(TaskNode wait) =>
            all || (script is not null ? wait.Script == script : owner.IsValid && wait.Owner == owner);
    }
}
