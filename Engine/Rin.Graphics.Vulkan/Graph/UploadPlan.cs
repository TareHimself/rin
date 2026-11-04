using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

internal enum UploadStepKind
{
    TextureWrite,
    BufferWrites,
    Copy
}

/// <summary>
///     One upload or copy pass to synthesize: a texture write, a run of buffer writes between fences, or a copy.
/// </summary>
internal sealed class UploadStep(UploadStepKind kind, ResourceHandle handle, int laneIndex, PendingResourceAction? action)
{
    public UploadStepKind Kind { get; } = kind;

    /// <summary>
    ///     The lane of a write, or the source lane of a copy.
    /// </summary>
    public ResourceHandle Handle { get; } = handle;

    public int LaneIndex { get; } = laneIndex;

    /// <summary>
    ///     The peeked texture write or the copy, null for a run of buffer writes.
    /// </summary>
    public PendingResourceAction? Action { get; } = action;

    /// <summary>
    ///     How many lane items the step stands for.
    /// </summary>
    public int Count { get; internal set; } = 1;

    public int PreviousInLane { get; internal set; } = -1;
    public int PreviousInDestinationLane { get; internal set; } = -1;

    public ResourceHandle Destination => ((SharedWrite)Action!).Destination.Handle;
}

/// <summary>
///     Which lane items a graph flushes this frame, and in what order. Copies go first in global order, each with
///     the lane segments before its fences, then whatever remains of the lanes the graph touches. A step is only
///     chosen once everything before it in its lanes is, so any set of steps closed under "previous in lane" is a
///     prefix of every lane.
/// </summary>
internal sealed class UploadPlan
{
    private readonly Dictionary<ResourceHandle, Cursor> _cursors = [];
    private readonly Dictionary<ResourceHandle, int> _lastStep = [];
    private readonly HashSet<ResourceHandle> _active = [];
    private readonly UploadLanes _lanes;

    private UploadPlan(UploadLanes lanes)
    {
        _lanes = lanes;
    }

    public List<UploadStep> Steps { get; } = [];

    /// <summary>
    ///     Indices into <see cref="Steps" /> per lane, in lane order.
    /// </summary>
    public Dictionary<ResourceHandle, List<int>> LaneSteps { get; } = [];

    /// <summary>
    ///     Peeks the lanes of <paramref name="touched" /> and of whatever <paramref name="import" /> adds for a copy
    ///     that has only one side in the graph. It is given the shared write and the handle to import.
    /// </summary>
    public static UploadPlan Build(UploadLanes lanes, IEnumerable<ResourceHandle> touched,
        Func<SharedWrite, ResourceHandle, bool> import)
    {
        var plan = new UploadPlan(lanes);
        foreach (var handle in touched) plan._active.Add(handle);
        plan.PlanCopies(import);
        plan.PlanRemainingSegments();
        return plan;
    }

    private void PlanCopies(Func<SharedWrite, ResourceHandle, bool> import)
    {
        List<SharedWrite> shared = [];
        _lanes.PeekShared(shared);
        foreach (var write in shared)
        {
            var sourceHandle = write.Source.Handle;
            var destinationHandle = write.Destination.Handle;
            if (!_active.Contains(sourceHandle) && !_active.Contains(destinationHandle)) continue;

            var source = CursorFor(sourceHandle);
            var destination = CursorFor(destinationHandle);
            if (!source.IsNextFence(write) || !destination.IsNextFence(write)) continue;
            if (!Activate(write, sourceHandle, import) || !Activate(write, destinationHandle, import)) continue;

            SelectSegment(sourceHandle, source);
            SelectSegment(destinationHandle, destination);
            AddShared(write, source, destination);
        }
    }

    private void PlanRemainingSegments()
    {
        foreach (var handle in _active)
            SelectSegment(handle, CursorFor(handle));
    }

    private bool Activate(SharedWrite write, in ResourceHandle handle, Func<SharedWrite, ResourceHandle, bool> import)
    {
        return _active.Contains(handle) || (import(write, handle) && _active.Add(handle));
    }

    private Cursor CursorFor(in ResourceHandle handle)
    {
        if (_cursors.TryGetValue(handle, out var cursor)) return cursor;
        cursor = new Cursor();
        _lanes.Peek(handle, cursor.Items);
        _cursors[handle] = cursor;
        return cursor;
    }

    private void SelectSegment(in ResourceHandle handle, Cursor cursor)
    {
        var end = cursor.NextFenceIndex();
        UploadStep? run = null;
        for (var i = cursor.Selected; i < end; i++)
        {
            switch (cursor.Items[i])
            {
                case PendingBufferWrite:
                    if (run is null)
                        run = AddWriteStep(new UploadStep(UploadStepKind.BufferWrites, handle, i, null));
                    else
                        run.Count++;
                    break;
                case PendingTextureWrite write:
                    run = null;
                    AddWriteStep(new UploadStep(UploadStepKind.TextureWrite, handle, i, write));
                    break;
            }
        }

        cursor.Selected = end;
    }

    private UploadStep AddWriteStep(UploadStep step)
    {
        step.PreviousInLane = _lastStep.GetValueOrDefault(step.Handle, -1);
        Register(step, step.Handle);
        return step;
    }

    private void AddShared(SharedWrite write, Cursor source, Cursor destination)
    {
        var sourceHandle = write.Source.Handle;
        var destinationHandle = write.Destination.Handle;
        var step = new UploadStep(UploadStepKind.Copy, sourceHandle, source.Selected, write)
        {
            PreviousInLane = _lastStep.GetValueOrDefault(sourceHandle, -1),
            PreviousInDestinationLane = write.IsSingleLane ? -1 : _lastStep.GetValueOrDefault(destinationHandle, -1)
        };
        Register(step, sourceHandle);
        source.Selected++;
        if (write.IsSingleLane) return;

        Register(step, destinationHandle, false);
        destination.Selected++;
    }

    private void Register(UploadStep step, in ResourceHandle lane, bool addStep = true)
    {
        if (addStep) Steps.Add(step);
        var index = Steps.Count - 1;
        _lastStep[lane] = index;
        if (!LaneSteps.TryGetValue(lane, out var steps)) LaneSteps[lane] = steps = [];
        steps.Add(index);
    }

    private sealed class Cursor
    {
        public List<PendingResourceAction> Items { get; } = [];
        public int Selected { get; set; }

        public int NextFenceIndex()
        {
            var index = Selected;
            while (index < Items.Count && Items[index] is not SharedWrite) index++;
            return index;
        }

        public bool IsNextFence(SharedWrite fence)
        {
            var index = NextFenceIndex();
            return index < Items.Count && ReferenceEquals(Items[index], fence);
        }
    }
}
