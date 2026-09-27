using System.Diagnostics;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphBuilder(IResourcePool resourcePool, Frame frame) : IGraphBuilder
{
    private readonly Dictionary<ResourceHandle, List<uint>> _externalBufferIds = [];
    private readonly Dictionary<ResourceHandle, uint> _externalImageIds = [];
    private readonly Dictionary<uint, IResourceDescriptor> _externalResources = [];

    private readonly Dictionary<uint, IPass> _passes = [];
    private readonly Dictionary<object, object> _shared = [];
    private List<IDisposable> _disposables = [];
    private uint _latestId;
    private uint _swapchainImageId;


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

        if (!_passes.Values.Any(pass => pass is ITerminalPass))
        {
            foreach (var pass in _passes.Values) (pass as IDisposable)?.Dispose();
            foreach (var disposable in _disposables) disposable.Dispose();
            _disposables.Clear();
            foreach (var descriptor in config.Resources.Values)
                (descriptor as IExternalResourceDescriptor)?.Resource.Dispose();
            return null;
        }

        var (nodes, passParents) = CollectLivePasses(config);

        foreach (var passId in _passes.Keys)
            if (!nodes.ContainsKey(passId))
                (_passes[passId] as IDisposable)?.Dispose();

        TakeUploads(uploads, nodes);

        foreach (var actions in config.ResourceActions.Values) actions.RemoveAll(a => !nodes.ContainsKey(a.PassId));

        var syncGroups = BuildSyncs(config);
        var executionGroups = BuildExecutionGroups(ScheduleLevels(nodes, passParents), syncGroups);
        var resources = CollectLiveResources(config);

        // The builder is reused every frame, so ownership of this frame's disposables moves to the graph.
        var disposables = _disposables;
        _disposables = [];
        return new CompiledGraph(resourcePool, frame, resources, executionGroups, disposables);
    }

    /// <summary>
    ///     Walks back from the terminal passes; anything not reached is pruned. Returns each live pass's
    ///     dependencies and, inverted, the passes that depend on it.
    /// </summary>
    private (Dictionary<uint, HashSet<uint>> Nodes, Dictionary<uint, HashSet<uint>> Parents) CollectLivePasses(
        GraphConfig config)
    {
        Dictionary<uint, HashSet<uint>> nodes = [];
        Dictionary<uint, HashSet<uint>> passParents = [];
        var toCheck = new Queue<uint>();
        var visited = new HashSet<uint>();
        foreach (var pass in _passes.Values)
        {
            if (pass is not ITerminalPass) continue;
            toCheck.Enqueue(pass.Id);
            visited.Add(pass.Id);
        }

        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            HashSet<uint> dependencies = [];
            nodes[passId] = dependencies;

            void DependOn(uint dependencyId)
            {
                dependencies.Add(dependencyId);
                if (!passParents.TryGetValue(dependencyId, out var parents))
                    passParents[dependencyId] = parents = [];
                parents.Add(passId);
                if (visited.Add(dependencyId)) toCheck.Enqueue(dependencyId);
            }

            if (!config.PassDependencies.TryGetValue(passId, out var passDependencies)) continue;

            foreach (var dependency in passDependencies)
                switch (dependency.Type)
                {
                    case GraphConfig.DependencyType.Pass:
                        DependOn(dependency.Id);
                        break;
                    case GraphConfig.DependencyType.Read:
                    {
                        // A read depends on the last write before it.
                        var resourceActions = config.ResourceActions[dependency.Id];
                        var readIdx = resourceActions.FindLastIndex(c =>
                            c.PassId == passId && c.Operation == ResourceOperation.Read);
                        for (var i = readIdx - 1; i > -1; i--)
                            if (resourceActions[i].Operation == ResourceOperation.Write)
                            {
                                DependOn(resourceActions[i].PassId);
                                break;
                            }
                    }
                        break;
                    case GraphConfig.DependencyType.Write:
                    {
                        // A write waits for every read since the previous write, and that write itself.
                        var resourceActions = config.ResourceActions[dependency.Id];
                        if (resourceActions[0].PassId == passId) break;

                        var writeIdx = resourceActions.FindLastIndex(c =>
                            c.PassId == passId && c.Operation == ResourceOperation.Write);
                        for (var i = writeIdx - 1; i > -1; i--)
                        {
                            DependOn(resourceActions[i].PassId);
                            if (resourceActions[i].Operation == ResourceOperation.Write) break;
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        return (nodes, passParents);
    }

    /// <summary>
    ///     The barriers each live pass needs before it runs, from consecutive actions on each resource.
    /// </summary>
    private Dictionary<uint, List<PassResourceSync>> BuildSyncs(GraphConfig config)
    {
        var syncGroups = new Dictionary<uint, List<PassResourceSync>>();

        void Add(PassResourceSync sync)
        {
            if (!syncGroups.TryGetValue(sync.PassId, out var group)) syncGroups[sync.PassId] = group = [];
            group.Add(sync);
        }

        foreach (var (resourceId, actions) in config.ResourceActions)
        {
            GraphConfig.ResourceAction? lastAction = null;
            foreach (var action in actions)
            {
                if (action.Type == GraphResourceKind.Buffer)
                {
                    if (lastAction != null &&
                        (action.Operation != lastAction.Operation || action.Operation == ResourceOperation.Write))
                        Add(new BufferResourceSync
                        {
                            PreviousUsage = lastAction.BufferUsage,
                            NextUsage = action.BufferUsage,
                            PreviousOperation = lastAction.Operation,
                            NextOperation = action.Operation,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        });
                }
                else if (lastAction == null && action.ImageLayout != ImageLayout.Undefined)
                {
                    // External images carry their layout over from earlier frames, unless the write discards it.
                    Add(new ImageResourceSync
                    {
                        PreviousLayout = ImageLayout.Undefined,
                        FromCurrentLayout = !action.Discard && resourceId != _swapchainImageId &&
                                            config.Resources[resourceId] is IExternalResourceDescriptor,
                        NextLayout = action.ImageLayout,
                        PreviousOperation = ResourceOperation.Write,
                        NextOperation = action.Operation,
                        ResourceId = resourceId,
                        PassId = action.PassId
                    });
                }
                else if (lastAction != null && (action.Operation == ResourceOperation.Write ||
                                                action.ImageLayout != lastAction.ImageLayout ||
                                                action.Operation != lastAction.Operation))
                {
                    Add(new ImageResourceSync
                    {
                        PreviousLayout = lastAction.ImageLayout,
                        NextLayout = action.ImageLayout,
                        PreviousOperation = lastAction.Operation,
                        NextOperation = action.Operation,
                        ResourceId = resourceId,
                        PassId = action.PassId
                    });
                }

                lastAction = action;
            }
        }

        return syncGroups;
    }

    /// <summary>
    ///     Groups live passes by depth, each one level past its deepest dependency, so a level only depends on
    ///     earlier levels.
    /// </summary>
    private List<IPass>[] ScheduleLevels(Dictionary<uint, HashSet<uint>> nodes,
        Dictionary<uint, HashSet<uint>> passParents)
    {
        var executionLevels = new Dictionary<uint, int>();
        var queue = new Queue<uint>();
        var remainingDependencies = new Dictionary<uint, int>(nodes.Count);
        foreach (var (passId, dependencies) in nodes)
        {
            remainingDependencies[passId] = dependencies.Count;
            if (dependencies.Count != 0) continue;
            queue.Enqueue(passId);
            executionLevels[passId] = 0;
        }

        var maxLevel = 0;
        while (queue.Count > 0)
        {
            var passId = queue.Dequeue();
            if (!passParents.TryGetValue(passId, out var parents)) continue;

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

        var levels = new List<IPass>[maxLevel + 1];
        for (var i = 0; i < levels.Length; i++) levels[i] = [];
        foreach (var (passId, level) in executionLevels) levels[level].Add(_passes[passId]);
        return levels;
    }

    /// <summary>
    ///     Puts a <see cref="BarrierPass" /> holding a level's syncs in front of that level.
    /// </summary>
    private List<ExecutionGroup> BuildExecutionGroups(List<IPass>[] levels,
        Dictionary<uint, List<PassResourceSync>> syncGroups)
    {
        var executionGroups = new List<ExecutionGroup>();
        foreach (var level in levels)
        {
            var bufferSyncs = new List<BufferResourceSync>();
            var imageSyncs = new List<ImageResourceSync>();
            foreach (var pass in level)
            {
                if (!syncGroups.TryGetValue(pass.Id, out var syncGroup)) continue;
                bufferSyncs.AddRange(syncGroup.OfType<BufferResourceSync>());
                imageSyncs.AddRange(syncGroup.OfType<ImageResourceSync>());
            }

            if (imageSyncs.NotEmpty() || bufferSyncs.NotEmpty())
                executionGroups.Add(new ExecutionGroup
                {
                    Passes = [new BarrierPass(bufferSyncs.ToArray(), imageSyncs.ToArray()) { Id = MakeId() }]
                });

            executionGroups.Add(new ExecutionGroup { Passes = level });
        }

        return executionGroups;
    }

    /// <summary>
    ///     Keeps the descriptors of resources a live pass uses and releases externals nothing uses.
    /// </summary>
    private static Dictionary<uint, IResourceDescriptor> CollectLiveResources(GraphConfig config)
    {
        var resources = new Dictionary<uint, IResourceDescriptor>(config.ResourceActions.Count);
        foreach (var id in config.ResourceActions.Keys) resources[id] = config.Resources[id];

        foreach (var (id, descriptor) in config.Resources)
            if (descriptor is IExternalResourceDescriptor external && !resources.ContainsKey(id))
                external.Resource.Dispose();

        return resources;
    }

    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null)
    {
        if (TryReuseExternalImage(handle, onDispose, out var existingId)) return existingId;
        if (ExternalResourceDescriptors.Make(handle, onDispose) is not { } descriptor) return 0;
        var id = MakeId();
        _externalResources.Add(id, descriptor);
        RememberExternalImage(handle, id);
        return id;
    }

    internal bool TryReuseExternalImage(in ResourceHandle handle, Action? onDispose, out uint id)
    {
        if (!_externalImageIds.TryGetValue(handle, out id)) return false;
        if (onDispose is not null) AddDisposable(new DisposeCallback(onDispose));
        return true;
    }

    internal void RememberExternalImage(in ResourceHandle handle, uint id)
    {
        _externalImageIds.Add(handle, id);
    }

    public uint AddExternalBuffer(in DeviceBufferView view, Action? onDispose = null)
    {
        if (ExternalResourceDescriptors.MakeBuffer(view, onDispose) is not { } descriptor) return 0;
        var id = MakeId();
        _externalResources.Add(id, descriptor);
        RememberExternalBuffer(view.Buffer, id);
        return id;
    }

    internal void RememberExternalBuffer(in ResourceHandle handle, uint id)
    {
        if (!_externalBufferIds.TryGetValue(handle, out var ids)) _externalBufferIds[handle] = ids = [];
        ids.Add(id);
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

    public uint AddDestinationImage(ResourceHandle handle, Action? onDispose = null)
    {
        return _swapchainImageId = AddExternalImage(handle, onDispose);
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
        _swapchainImageId = 0;
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

    private Uploads SynthesizeUploads(GraphConfig config)
    {
        var module = VulkanGraphicsModule.Get();
        var uploads = new Uploads([], []);

        foreach (var (handle, resourceId) in _externalImageIds)
        {
            if (handle.Type != ResourceType.Texture || resourceId == _swapchainImageId ||
                !config.ResourceActions.ContainsKey(resourceId)) continue;

            var count = module.PeekTextureWrites(handle, out var firstCoversWholeImage);
            if (count == 0) continue;

            var passes = new TextureUploadPass[count];
            for (var i = 0; i < count; i++)
            {
                passes[i] = new TextureUploadPass(handle, resourceId, i == 0 && firstCoversWholeImage);
                AddSynthesizedPass(config, passes[i]);
            }

            uploads.Textures.Add(passes);
        }

        foreach (var (handle, resourceIds) in _externalBufferIds)
        {
            var usedIds = resourceIds.Where(config.ResourceActions.ContainsKey).ToArray();
            if (usedIds.Length == 0 || !module.HasBufferWrites(handle)) continue;

            var pass = new BufferUploadPass(handle, usedIds);
            AddSynthesizedPass(config, pass);
            uploads.Buffers.Add(pass);
        }

        return uploads;
    }

    private void AddSynthesizedPass(GraphConfig config, IPass pass)
    {
        pass.Id = MakeId();
        _passes.Add(pass.Id, pass);
        config.CurrentPassId = pass.Id;
        pass.Configure(config);
    }

    private void TakeUploads(Uploads uploads, Dictionary<uint, HashSet<uint>> nodes)
    {
        var module = VulkanGraphicsModule.Get();
        List<(TextureUploadPass Pass, PendingTextureWrite Write)> textureWrites = [];
        List<(BufferUploadPass Pass, List<PendingBufferWrite> Writes)> bufferWrites = [];
        ulong stagingSize = 0;

        List<PendingTextureWrite> taken = [];
        foreach (var passes in uploads.Textures)
        {
            if (!nodes.ContainsKey(passes[^1].Id))
            {
                continue;
            }

            taken.Clear();
            module.TakeTextureWrites(passes[0].Texture, passes.Length, taken);
            for (var i = 0; i < taken.Count; i++)
            {
                textureWrites.Add((passes[i], taken[i]));
                stagingSize += taken[i].StagingSize;
            }
        }

        foreach (var pass in uploads.Buffers)
        {
            if (!nodes.ContainsKey(pass.Id))
            {
                continue;
            }

            List<PendingBufferWrite> writes = [];
            module.TakeBufferWrites(pass.Buffer, writes);
            if (writes.Count == 0) continue;
            bufferWrites.Add((pass, writes));
            stagingSize += WriteRecorder.StagingSize(writes);
        }

        if (stagingSize == 0) return;

        var staging = WriteRecorder.CreateStaging(module, stagingSize);
        AddDisposable(new DisposeCallback(() => module.FreeResourceHandles(staging)));

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
        Debug.Assert(_swapchainImageId != 0, "A swapchain image must be added to the graph");

        var config = new GraphConfig(this)
        {
            DestinationImageId = _swapchainImageId
        };

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

    private sealed record Uploads(List<TextureUploadPass[]> Textures, List<BufferUploadPass> Buffers);

    private sealed class DisposeCallback(Action callback) : IDisposable
    {
        public void Dispose()
        {
            callback();
        }
    }
}
