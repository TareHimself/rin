using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<ResourceHandle, List<uint>> _externalBufferIds = [];
    private readonly Dictionary<ResourceHandle, uint> _externalImageIds = [];
    private readonly Dictionary<uint, IResourceDescriptor> _externalResources = [];

    private readonly Dictionary<uint, IPass> _passes = [];
    private readonly Dictionary<object, object> _shared = [];
    private readonly HashSet<uint> _terminalResources = [];
    private readonly List<IDisposable> _disposables = [];
    private readonly GraphCompileState _state = new();
    private readonly GraphConfig _config;
    private CompiledGraph? _spareGraph;
    private uint _latestId;
    private readonly IResourcePool _resourcePool;
    private readonly Frame _frame;
    private readonly UploadLanes _pendingActions;
    private readonly IStagingAllocator _staging;

    internal GraphBuilder(IResourcePool resourcePool, Frame frame, UploadLanes pendingActions,
        IStagingAllocator staging, IExternalResourceSource externalResources)
    {
        _resourcePool = resourcePool;
        _frame = frame;
        _pendingActions = pendingActions;
        _staging = staging;
        ExternalResources = externalResources;
        _config = new GraphConfig(this);
    }

    internal IExternalResourceSource ExternalResources { get; }

    public uint AddPass(IPass pass)
    {
        {
            if (pass is IPassWithPreAdd p) p.PreAdd(this);
        }

        var passId = MakeId();
        pass.Id = passId;
        _passes.Add(passId, pass);

        {
            if (pass is IPassWithPostAdd p) p.PostAdd(this);
        }
        return passId;
    }

    public ICompiledGraph? Compile()
    {
        var config = Configure();
        var uploads = SynthesizeUploads(config);
        _externalResources.Clear();
        _externalImageIds.Clear();
        _externalBufferIds.Clear();

        _state.Clear();
        CollectRoots(config);
        if (_state.Roots.Count == 0)
        {
            foreach (var pass in _passes.Values) (pass as IDisposable)?.Dispose();
            foreach (var disposable in _disposables) disposable.Dispose();
            _disposables.Clear();
            foreach (var descriptor in config.Resources.Values)
                (descriptor as IExternalResourceDescriptor)?.Resource.Dispose();
            return null;
        }

        CollectLivePasses(config);

        foreach (var (passId, pass) in _passes)
            if (!_state.IsLive(passId))
                (pass as IDisposable)?.Dispose();

        TakeUploads(uploads);

        foreach (var actions in config.ResourceActions.Values) RemoveDeadActions(actions);

        BuildSyncs(config);
        var levelCount = ScheduleLevels();

        var graph = TakeGraph();
        BuildExecutionGroups(graph, levelCount);
        CollectLiveResources(config, graph.Descriptors);

        // The builder is reused every frame, so ownership of this frame's disposables moves to the graph.
        graph.Disposables.AddRange(_disposables);
        _disposables.Clear();
        return graph;
    }

    private CompiledGraph TakeGraph()
    {
        var graph = _spareGraph ?? new CompiledGraph(_resourcePool, _frame, this);
        _spareGraph = null;
        graph.Reopen();
        return graph;
    }

    /// <summary>
    ///     Takes back a disposed graph, to fill again on the next compile.
    /// </summary>
    internal void Recycle(CompiledGraph graph)
    {
        _spareGraph = graph;
    }

    /// <summary>
    ///     The pruning roots: terminal passes and every pass that writes a terminal resource.
    /// </summary>
    private void CollectRoots(GraphConfig config)
    {
        var roots = _state.Roots;
        foreach (var pass in _passes.Values)
            if (pass is ITerminalPass)
                roots.Add(pass.Id);

        foreach (var resourceId in _terminalResources)
        {
            if (!config.ResourceActions.TryGetValue(resourceId, out var actions)) continue;
            foreach (var action in actions)
                if (action.Operation == ResourceOperation.Write)
                    roots.Add(action.PassId);
        }
    }

    /// <summary>
    ///     Walks back from the roots; anything not reached is pruned. Records each live pass's
    ///     dependencies and, inverted, the passes that depend on it.
    /// </summary>
    private void CollectLivePasses(GraphConfig config)
    {
        var toCheck = _state.Pending;
        foreach (var root in _state.Roots)
        {
            toCheck.Enqueue(root);
            _state.Visited.Add(root);
        }

        while (toCheck.TryDequeue(out var passId))
        {
            var dependencies = _state.AddLivePass(passId);
            if (!config.PassDependencies.TryGetValue(passId, out var passDependencies)) continue;

            foreach (var dependency in passDependencies)
                switch (dependency.Type)
                {
                    case GraphConfig.DependencyType.Pass:
                        DependOn(passId, dependencies, dependency.Id);
                        break;
                    case GraphConfig.DependencyType.Read:
                    {
                        // A read depends on the last write before it.
                        var resourceActions = config.ResourceActions[dependency.Id];
                        var readIdx = LastActionIndex(resourceActions, passId, ResourceOperation.Read);
                        for (var i = readIdx - 1; i > -1; i--)
                            if (resourceActions[i].Operation == ResourceOperation.Write)
                            {
                                DependOn(passId, dependencies, resourceActions[i].PassId);
                                break;
                            }
                    }
                        break;
                    case GraphConfig.DependencyType.Write:
                    {
                        // A write waits for every read since the previous write, and that write itself.
                        var resourceActions = config.ResourceActions[dependency.Id];
                        if (resourceActions[0].PassId == passId) break;

                        var writeIdx = LastActionIndex(resourceActions, passId, ResourceOperation.Write);
                        for (var i = writeIdx - 1; i > -1; i--)
                        {
                            DependOn(passId, dependencies, resourceActions[i].PassId);
                            if (resourceActions[i].Operation == ResourceOperation.Write) break;
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }
    }

    private void DependOn(uint passId, HashSet<uint> dependencies, uint dependencyId)
    {
        dependencies.Add(dependencyId);
        _state.AddDependent(dependencyId, passId);
        if (_state.Visited.Add(dependencyId)) _state.Pending.Enqueue(dependencyId);
    }

    private static int LastActionIndex(List<GraphConfig.ResourceAction> actions, uint passId,
        ResourceOperation operation)
    {
        for (var i = actions.Count - 1; i > -1; i--)
            if (actions[i].PassId == passId && actions[i].Operation == operation)
                return i;
        return -1;
    }

    private void RemoveDeadActions(List<GraphConfig.ResourceAction> actions)
    {
        var kept = 0;
        for (var i = 0; i < actions.Count; i++)
            if (_state.IsLive(actions[i].PassId))
                actions[kept++] = actions[i];
        actions.RemoveRange(kept, actions.Count - kept);
    }

    /// <summary>
    ///     The barriers each live pass needs before it runs, from consecutive actions on each resource.
    /// </summary>
    private void BuildSyncs(GraphConfig config)
    {
        foreach (var (resourceId, actions) in config.ResourceActions)
        {
            var descriptor = config.Resources[resourceId];
            var externalBuffer = (descriptor as ExternalVulkanBufferResourceDescriptor)?.Resource;
            var hasLast = false;
            GraphConfig.ResourceAction last = default;
            foreach (var action in actions)
            {
                if (action.Type == GraphResourceKind.Buffer)
                {
                    if (hasLast && (action.Operation != last.Operation || action.Operation == ResourceOperation.Write))
                        _state.AddSync(action.PassId, new BufferResourceSync(
                            resourceId,
                            PreviousUsage: last.BufferUsage,
                            NextUsage: action.BufferUsage,
                            PreviousOperation: last.Operation,
                            NextOperation: action.Operation));
                    else if (!hasLast && externalBuffer is { GraphState: { } previous } &&
                             (action.Operation == ResourceOperation.Write || previous.Operation == ResourceOperation.Write))
                        // External buffers carry their last usage over from earlier graphs.
                        _state.AddSync(action.PassId, new BufferResourceSync(
                            resourceId,
                            PreviousUsage: previous.Usage,
                            NextUsage: action.BufferUsage,
                            PreviousOperation: previous.Operation,
                            NextOperation: action.Operation));
                }
                else if (!hasLast && action.ImageLayout != ImageLayout.Undefined)
                {
                    // External images carry their layout over from earlier frames, unless the write discards it.
                    _state.AddSync(action.PassId, new ImageResourceSync(
                        resourceId,
                        PreviousLayout: ImageLayout.Undefined,
                        NextLayout: action.ImageLayout,
                        NextOperation: action.Operation,
                        FromCurrentLayout: !action.Discard && descriptor is IExternalResourceDescriptor));
                }
                else if (hasLast && (action.Operation == ResourceOperation.Write ||
                                     action.ImageLayout != last.ImageLayout ||
                                     action.Operation != last.Operation))
                {
                    _state.AddSync(action.PassId, new ImageResourceSync(
                        resourceId,
                        PreviousLayout: last.ImageLayout,
                        NextLayout: action.ImageLayout,
                        NextOperation: action.Operation));
                }

                last = action;
                hasLast = true;
            }

            if (externalBuffer != null && hasLast && last.Type == GraphResourceKind.Buffer)
                externalBuffer.GraphState = new BufferGraphState(last.BufferUsage, last.Operation);
        }
    }

    /// <summary>
    ///     Groups live passes by depth, each one level past its deepest dependency, so a level only depends on
    ///     earlier levels. Returns the number of levels.
    /// </summary>
    private int ScheduleLevels()
    {
        var executionLevels = _state.Levels;
        var queue = _state.Pending;
        var remainingDependencies = _state.RemainingDependencies;
        foreach (var (passId, dependencies) in _state.Dependencies)
        {
            remainingDependencies[passId] = dependencies.Count;
            if (dependencies.Count != 0) continue;
            queue.Enqueue(passId);
            executionLevels[passId] = 0;
        }

        var maxLevel = 0;
        while (queue.TryDequeue(out var passId))
        {
            if (!_state.Dependents.TryGetValue(passId, out var parents)) continue;

            var nextLevel = executionLevels[passId] + 1;
            foreach (var parentId in parents)
            {
                if (!executionLevels.TryGetValue(parentId, out var existingLevel) || nextLevel > existingLevel)
                {
                    executionLevels[parentId] = nextLevel;
                    maxLevel = Math.Max(maxLevel, nextLevel);
                }

                if (--remainingDependencies[parentId] == 0) queue.Enqueue(parentId);
            }
        }

        var levelCount = maxLevel + 1;
        var passesByLevel = _state.PassesByLevel;
        while (passesByLevel.Count < levelCount) passesByLevel.Add([]);
        foreach (var (passId, level) in executionLevels) passesByLevel[level].Add(_passes[passId]);
        return levelCount;
    }

    /// <summary>
    ///     Puts a <see cref="BarrierPass" /> holding a level's syncs in front of that level.
    /// </summary>
    private void BuildExecutionGroups(CompiledGraph graph, int levelCount)
    {
        for (var i = 0; i < levelCount; i++)
        {
            var level = _state.PassesByLevel[i];
            BarrierPass? barrier = null;
            foreach (var pass in level)
            {
                if (_state.BufferSyncs.TryGetValue(pass.Id, out var bufferSyncs))
                    (barrier ??= graph.AddBarrier(MakeId())).Buffers.AddRange(bufferSyncs);
                if (_state.ImageSyncs.TryGetValue(pass.Id, out var imageSyncs))
                    (barrier ??= graph.AddBarrier(MakeId())).Images.AddRange(imageSyncs);
            }

            graph.AddGroup().Passes.AddRange(level);
        }
    }

    /// <summary>
    ///     Keeps the descriptors of resources a live pass uses and releases externals nothing uses.
    /// </summary>
    private static void CollectLiveResources(GraphConfig config, Dictionary<uint, IResourceDescriptor> resources)
    {
        foreach (var id in config.ResourceActions.Keys) resources[id] = config.Resources[id];

        foreach (var (id, descriptor) in config.Resources)
            if (descriptor is IExternalResourceDescriptor external && !resources.ContainsKey(id))
                external.Resource.Dispose();
    }

    public uint AddExternalImage(ResourceHandle handle, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null)
    {
        if (TryReuseExternalImage(handle, flags, onDispose, out var existingId)) return existingId;
        if (ExternalResourceDescriptors.Make(ExternalResources, handle, onDispose) is not { } descriptor) return 0;
        var id = MakeId();
        _externalResources.Add(id, descriptor);
        RememberExternalImage(handle, id, flags);
        return id;
    }

    internal bool TryReuseExternalImage(in ResourceHandle handle, GraphResourceFlags flags, Action? onDispose,
        out uint id)
    {
        if (!_externalImageIds.TryGetValue(handle, out id)) return false;
        RememberFlags(id, flags);
        if (onDispose is not null) AddDisposable(new DisposeCallback(onDispose));
        return true;
    }

    internal void RememberExternalImage(in ResourceHandle handle, uint id, GraphResourceFlags flags)
    {
        _externalImageIds.Add(handle, id);
        RememberFlags(id, flags);
    }

    public uint AddExternalBuffer(in DeviceBufferView view, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null)
    {
        if (ExternalResourceDescriptors.MakeBuffer(ExternalResources, view, onDispose) is not { } descriptor) return 0;
        var id = MakeId();
        _externalResources.Add(id, descriptor);
        RememberExternalBuffer(view.Buffer, id, flags);
        return id;
    }

    internal void RememberExternalBuffer(in ResourceHandle handle, uint id, GraphResourceFlags flags)
    {
        if (!_externalBufferIds.TryGetValue(handle, out var ids)) _externalBufferIds[handle] = ids = [];
        ids.Add(id);
        RememberFlags(id, flags);
    }

    internal void RememberFlags(uint id, GraphResourceFlags flags)
    {
        if ((flags & GraphResourceFlags.Terminal) != 0) _terminalResources.Add(id);
    }

    public void AddDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public T GetOrAddShared<T>(object key, Func<IGraphBuilder, T> create) where T : class
    {
        if (_shared.TryGetValue(key, out var existing)) return (T)existing;
        var created = create(this);
        _shared[key] = created;
        if (created is IDisposable disposable) AddDisposable(disposable);
        return created;
    }

    public void Reset()
    {
        // Anything still here was registered for a frame that never compiled (e.g. swapchain acquire failed).
        foreach (var descriptor in _externalResources.Values)
            (descriptor as IExternalResourceDescriptor)?.Resource.Dispose();
        foreach (var disposable in _disposables) disposable.Dispose();
        _disposables.Clear();
        // _images.Clear();
        // _memory.Clear();
        _passes.Clear();
        _externalResources.Clear();
        _externalImageIds.Clear();
        _externalBufferIds.Clear();
        _shared.Clear();
        _terminalResources.Clear();
        _latestId = 0;
    }

    /// <summary>
    ///     All Resource ID's must be greater than zero
    /// </summary>
    /// <returns></returns>
    public uint MakeId()
    {
        return ++_latestId;
    }

    private UploadSynthesis? SynthesizeUploads(GraphConfig config)
    {
        if (_externalImageIds.Count == 0 && _externalBufferIds.Count == 0) return null;

        var resourceIds = CollectTouchedResourceIds(config);
        var plan = UploadPlan.Build(_pendingActions, resourceIds.Keys,
            (shared, handle) => TryImportForCopy(config, shared.Action, handle, resourceIds));
        var passes = new IUploadPass[plan.Steps.Count];
        for (var i = 0; i < passes.Length; i++)
        {
            var step = plan.Steps[i];
            passes[i] = MakeUploadPass(step, resourceIds);
            AddSynthesizedPass(config, passes[i]);
            if (step.PreviousInLane >= 0) config.DependOn(passes[step.PreviousInLane].Id);
            if (step.PreviousInDestinationLane >= 0) config.DependOn(passes[step.PreviousInDestinationLane].Id);
        }

        return new UploadSynthesis(plan, passes);
    }

    private Dictionary<ResourceHandle, uint[]> CollectTouchedResourceIds(GraphConfig config)
    {
        Dictionary<ResourceHandle, uint[]> resourceIds = [];
        foreach (var (handle, resourceId) in _externalImageIds)
            if (handle.Type == ResourceType.Texture && config.ResourceActions.ContainsKey(resourceId))
                resourceIds[handle] = [resourceId];

        foreach (var (handle, ids) in _externalBufferIds)
        {
            var used = 0;
            foreach (var id in ids)
                if (config.ResourceActions.ContainsKey(id))
                    used++;
            if (used == 0) continue;

            var usedIds = new uint[used];
            var next = 0;
            foreach (var id in ids)
                if (config.ResourceActions.ContainsKey(id))
                    usedIds[next++] = id;
            resourceIds[handle] = usedIds;
        }

        return resourceIds;
    }

    private bool TryImportForCopy(GraphConfig config, CopyAction copy, in ResourceHandle handle,
        Dictionary<ResourceHandle, uint[]> resourceIds)
    {
        switch (handle.Type)
        {
            case ResourceType.Texture:
            {
                var id = config.AddExternalImage(handle);
                if (id == 0) return false;
                resourceIds[handle] = [id];
                return true;
            }
            case ResourceType.Buffer:
            {
                if (!_externalBufferIds.TryGetValue(handle, out var ids))
                {
                    var id = config.AddExternalBuffer(CopiedRange(copy, handle));
                    if (id == 0) return false;
                    ids = [id];
                }

                resourceIds[handle] = ids.ToArray();
                return true;
            }
            default:
                return false;
        }
    }

    private static DeviceBufferView CopiedRange(CopyAction copy, in ResourceHandle handle)
    {
        return copy switch
        {
            BufferCopy buffers => new DeviceBufferView(handle,
                handle.Equals(buffers.Source) ? buffers.SourceOffset : buffers.DestinationOffset, buffers.Size),
            BufferToTextureCopy toTexture => new DeviceBufferView(handle, toTexture.SourceOffset,
                toTexture.Size),
            TextureToBufferCopy toBuffer => new DeviceBufferView(handle, toBuffer.DestinationOffset, toBuffer.Size),
            _ => throw new ArgumentOutOfRangeException(nameof(copy), copy, "Copy has no buffer side")
        };
    }

    private static IUploadPass MakeUploadPass(UploadStep step, Dictionary<ResourceHandle, uint[]> resourceIds)
    {
        var ids = resourceIds[step.Handle];
        return step.Kind switch
        {
            UploadStepKind.TextureWrite => new TextureUploadPass(step.Handle, ids[0],
                step.LaneIndex == 0 && ((PendingTextureWrite)step.Action!).CoversWholeImage),
            UploadStepKind.BufferWrites => new BufferUploadPass(step.Handle, ids),
            UploadStepKind.Copy => new CopyUploadPass((SharedWrite)step.Action!, ids,
                resourceIds[step.Destination]),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step.Kind, null)
        };
    }

    private void AddSynthesizedPass(GraphConfig config, IPass pass)
    {
        pass.Id = MakeId();
        _passes.Add(pass.Id, pass);
        config.CurrentPassId = pass.Id;
        pass.Configure(config);
    }

    private void TakeUploads(UploadSynthesis? uploads)
    {
        if (uploads is null) return;
        var (plan, passes) = uploads;
        List<(TextureUploadPass Pass, PendingTextureWrite Write)> textureWrites = [];
        List<(BufferUploadPass Pass, List<PendingBufferWrite> Writes)> bufferWrites = [];
        ulong stagingSize = 0;

        List<PendingResourceAction> taken = [];
        foreach (var (handle, stepIndices) in plan.LaneSteps)
        {
            var live = 0;
            var count = 0;
            foreach (var index in stepIndices)
            {
                if (!_state.IsLive(passes[index].Id)) break;
                live++;
                count += plan.Steps[index].Count;
            }

            if (live == 0) continue;

            taken.Clear();
            _pendingActions.Take(handle, count, taken);
            var next = 0;
            for (var i = 0; i < live && next < taken.Count; i++)
            {
                var step = plan.Steps[stepIndices[i]];
                switch (passes[stepIndices[i]])
                {
                    case TextureUploadPass texturePass:
                        var textureWrite = (PendingTextureWrite)taken[next++];
                        textureWrites.Add((texturePass, textureWrite));
                        stagingSize += textureWrite.StagingSize;
                        break;
                    case BufferUploadPass bufferPass:
                        List<PendingBufferWrite> writes = [];
                        for (var j = 0; j < step.Count && next < taken.Count; j++)
                            writes.Add((PendingBufferWrite)taken[next++]);
                        bufferWrites.Add((bufferPass, writes));
                        stagingSize += WriteRecorder.StagingSize(writes);
                        break;
                    case CopyUploadPass copyPass:
                        next++;
                        copyPass.Assign();
                        break;
                }
            }
        }

        if (stagingSize == 0) return;

        var staging = _staging.Allocate(stagingSize);
        AddDisposable(new DisposeCallback(() => _staging.Free(staging)));

        ulong offset = 0;
        foreach (var (pass, write) in textureWrites)
        {
            pass.Assign(write, new DeviceBufferView(staging, offset, write.StagingSize));
            offset += write.StagingSize;
        }

        foreach (var (pass, writes) in bufferWrites)
        {
            var size = WriteRecorder.StagingSize(writes);
            pass.Assign(writes, new DeviceBufferView(staging, offset, size));
            offset += size;
        }
    }

    private GraphConfig Configure()
    {
        var config = _config;
        config.Reset();

        foreach (var (id, externalImageResourceDescriptor) in _externalResources)
            config.Resources.Add(id, externalImageResourceDescriptor);

        foreach (var (id, pass) in _passes)
        {
            config.CurrentPassId = id;
            pass.Configure(config);
        }

        config.FillResources();

        return config;
    }

    private sealed record UploadSynthesis(UploadPlan Plan, IUploadPass[] Passes);

    private sealed class DisposeCallback(Action callback) : IDisposable
    {
        public void Dispose()
        {
            callback();
        }
    }
}
