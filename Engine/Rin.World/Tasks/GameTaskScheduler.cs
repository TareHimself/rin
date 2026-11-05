using System.Runtime.ExceptionServices;
using Rin.World.Scheduling;
using Rin.World.Scripting;

namespace Rin.World.Tasks;

/// <summary>
///     Runs <see cref="GameTask" />s on one thread. Waits are resumed only in <see cref="TickPhase.Tasks" />, in the
///     order they were queued, and a task whose owner entity is gone is cancelled instead of resumed.
/// </summary>
/// <remarks>
///     A cancelled task is resumed right away with a <see cref="GameTaskCanceledException" /> at its pending
///     <c>await</c>, so its <c>catch</c> and <c>finally</c> blocks run. Tasks are cancelled in the order they were
///     started. Any other exception escaping a task started with <see cref="Run" /> is reported at the end of the
///     Tasks phase to <see cref="TaskFaulted" />, or rethrown from it when nothing subscribed. A snapshot restore cancels
///     every task before the state is replaced, and drops every raised event that was not delivered yet.
/// </remarks>
public sealed class GameTaskScheduler : IWorldRestoreListener
{
    private readonly List<WaitEntry> _roots = [];
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
    ///     <paramref name="owner" /> is despawned, unless the owner is <c>default</c>. Throws during a snapshot restore.
    /// </summary>
    public void Run(Entity owner, Func<TaskContext, GameTask> body) => Run(owner, null, body);

    /// <summary>
    ///     Completes the <see cref="TaskContext.Event{T}" /> waits for <paramref name="target" /> in the next Tasks phase,
    ///     or in the current one when it has not started resuming yet. An event nobody waits for is dropped.
    /// </summary>
    public void Raise<T>(Entity target, in T value) where T : unmanaged => Channel<T>().Raise(target, value, _sequence++);

    /// <summary>
    ///     Cancels every task started for <paramref name="owner" /> and every task waiting on something it owns.
    /// </summary>
    public void Cancel(Entity owner) => Cancel(new CancelFilter(owner, null, false), CancelReason.Cancelled);

    public void CancelAll() => CancelAll(CancelReason.Cancelled);

    void IWorldRestoreListener.BeforeRestore()
    {
        CancelAll(CancelReason.Restored);
        for (var i = 0; i < _channels.Count; i++) _channels[i].Clear();
    }

    void IWorldRestoreListener.AfterRestore()
    {
    }

    internal void Run(Entity owner, Script? script, Func<TaskContext, GameTask> body)
    {
        if (World.IsRestoring) throw new InvalidOperationException("A GameTask cannot be started during a snapshot restore.");

        // A task started from a cancelled task's finally block is not part of that cancellation.
        var outer = CancellationScope.Enter(null);
        GameTask task;
        try
        {
            task = body(new TaskContext(this, owner, script));
        }
        finally
        {
            CancellationScope.Exit(outer);
        }

        if (task.TakeNode() is not { } node) return;

        node.MarkRoot(this);
        node.Owner = owner;
        node.Script = script;
        if (node.Status != TaskNodeStatus.Pending)
        {
            FinishRoot(node);
            return;
        }

        RunningCount++;
        _roots.Add(new WaitEntry(node));
    }

    internal void Cancel(Script script, CancelReason reason) => Cancel(new CancelFilter(default, script, false), reason);

    internal void CancelAll(CancelReason reason) => Cancel(new CancelFilter(default, null, true), reason);

    /// <summary>
    ///     Queues <paramref name="wait" />, or cancels it on the spot when a cancelled task makes it.
    /// </summary>
    internal GameTask Enqueue(TaskNode wait, in TaskContext context)
    {
        wait.Owner = context.Owner;
        wait.Script = context.Script;
        if (CancellationScope.Current is { } reason)
        {
            wait.Cancel(reason);
            return new GameTask(wait);
        }

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
            CompactRoots();
            CancelDespawnedRoots();
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
            if (!IsOwnerAlive(entry.Node)) entry.Node.Cancel(CancelReason.Despawned);
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
            if (IsOwnerAlive(entry.Node))
            {
                entry.Node.CompleteDue();
                continue;
            }

            // A task resumed earlier in this phase despawned the owner, so its tasks unwind in start order now.
            CancelDespawnedRoots();
            entry.Node.Cancel(CancelReason.Despawned);
        }
    }

    private void CompactRoots()
    {
        var kept = 0;
        for (var i = 0; i < _roots.Count; i++)
            if (_roots[i].IsPending) _roots[kept++] = _roots[i];
        _roots.RemoveRange(kept, _roots.Count - kept);
    }

    private void CancelDespawnedRoots()
    {
        var count = _roots.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = _roots[i];
            if (entry.IsPending && !IsOwnerAlive(entry.Node)) entry.Node.Cancel(CancelReason.Despawned);
        }
    }

    private bool IsOwnerAlive(TaskNode wait) => !wait.Owner.IsValid || World.IsAlive(wait.Owner);

    /// <summary>
    ///     Cancels the matching tasks in start order, then the matching waits of tasks not started with
    ///     <see cref="Run" />.
    /// </summary>
    private void Cancel(in CancelFilter filter, CancelReason reason)
    {
        Cancel(_roots, filter, reason);
        Cancel(_waiting, filter, reason);
        Cancel(_due, filter, reason);
    }

    // Tasks started by a finally block while this runs are past count, so they are not cancelled with it.
    private static void Cancel(List<WaitEntry> entries, in CancelFilter filter, CancelReason reason)
    {
        var count = entries.Count;
        for (var i = 0; i < count; i++)
        {
            var entry = entries[i];
            if (entry.IsPending && filter.Matches(entry.Node)) entry.Node.Cancel(reason);
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
    ///     A started task or a queued wait. The version tells a stale entry, for a node completed and released since,
    ///     from a live one.
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
        public bool Matches(TaskNode node) =>
            all || (script is not null ? node.Script == script : owner.IsValid && node.Owner == owner);
    }
}
