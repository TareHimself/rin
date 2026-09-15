using System.Diagnostics;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphBuilder(IResourcePool resourcePool, Frame frame) : IGraphBuilder
{
    private readonly Dictionary<uint, IResourceDescriptor> _externalResources = [];

    private readonly Dictionary<uint, IPass> _passes = [];
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

        // Nodes and their dependencies
        Dictionary<uint, HashSet<uint>> nodes = [];
        var toCheck = new Queue<uint>();
        var visited = new HashSet<uint>();
        foreach (var pass in _passes.Values)
        {
            if (!pass.IsTerminal) continue;
            toCheck.Enqueue(pass.Id);
            visited.Add(pass.Id);
        }

        if (toCheck.Empty())
        {
            foreach (var pass in _passes.Values) pass.OnPrune?.Invoke();
            return null;
        }

        // number of passes that depend on the key
        var passParents = new Dictionary<uint, HashSet<uint>>();

        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            HashSet<uint> dependencies = [];
            nodes[passId] = dependencies;
            if (config.PassDependencies.TryGetValue(passId, out var passDependencies))
                foreach (var dependency in passDependencies)
                    switch (dependency.Type)
                    {
                        case GraphConfig.DependencyType.Pass:
                        {
                            dependencies.Add(dependency.Id);
                            if (!passParents.ContainsKey(dependency.Id)) passParents.Add(dependency.Id, []);
                            passParents[dependency.Id].Add(passId);
                            if (!visited.Add(dependency.Id)) continue;
                            toCheck.Enqueue(dependency.Id);
                        }
                            break;
                        case GraphConfig.DependencyType.Read:
                        {
                            var resourceActions = config.ResourceActions[dependency.Id];

                            // Find the position of our read
                            var targetIdx = resourceActions.FindLastIndex(c =>
                                c.PassId == passId && c.Operation == ResourceOperation.Read);

                            // If found find the position of the write before it
                            if (targetIdx != -1)
                            {
                                GraphConfig.ResourceAction? targetAction = null;
                                for (var i = targetIdx - 1; i > -1; i--)
                                    if (resourceActions[i].Operation == ResourceOperation.Write)
                                    {
                                        targetAction = resourceActions[i];
                                        break;
                                    }

                                // If found add the write as a dependency and to the search
                                if (targetAction is { } action)
                                {
                                    dependencies.Add(action.PassId);
                                    if (!passParents.ContainsKey(action.PassId)) passParents.Add(action.PassId, []);
                                    passParents[action.PassId].Add(passId);
                                    if (!visited.Add(action.PassId)) continue;
                                    toCheck.Enqueue(action.PassId);
                                }
                            }
                        }
                            break;
                        case GraphConfig.DependencyType.Write:
                        {
                            var resourceActions = config.ResourceActions[dependency.Id];

                            // Check if we did not create this resource
                            if (resourceActions[0].PassId != passId)
                            {
                                // Find the position of our write
                                var targetIdx = resourceActions.FindLastIndex(c =>
                                    c.PassId == passId && c.Operation == ResourceOperation.Write);

                                // If found get all reads till the previous write
                                if (targetIdx != -1)
                                    // Write can't happen till all reads since last write happen
                                    for (var i = targetIdx - 1; i > -1; i--)
                                    {
                                        var action = resourceActions[i];

                                        if (!passParents.ContainsKey(action.PassId)) passParents.Add(action.PassId, []);
                                        passParents[action.PassId].Add(passId);
                                        dependencies.Add(action.PassId);

                                        if (visited.Add(action.PassId)) toCheck.Enqueue(action.PassId);

                                        if (resourceActions[i].Operation == ResourceOperation.Write) break;
                                    }
                            }
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
        }

        foreach (var passId in _passes.Keys)
            if (!nodes.ContainsKey(passId))
                _passes[passId].OnPrune?.Invoke();

        var finalResourceActions = new Dictionary<uint, List<GraphConfig.ResourceAction>>(config.ResourceActions.Count);
        foreach (var (resourceId, actions) in config.ResourceActions)
        {
            actions.RemoveAll(a => !nodes.ContainsKey(a.PassId));
            finalResourceActions[resourceId] = actions;
        }

        var syncGroups = new Dictionary<uint, List<PassResourceSync>>();
        // Sync just before use (could change to batch syncing)
        foreach (var (resourceId, actions) in finalResourceActions)
        {
            if (actions.Empty()) continue;
            GraphConfig.ResourceAction? lastAction = null;
            foreach (var action in actions)
            {
                if (action.Type == GraphResourceKind.Buffer)
                {
                    if (lastAction != null)
                        if (action.Operation != lastAction.Operation || action.Operation == ResourceOperation.Write)
                        {
                            var sync = new BufferResourceSync
                            {
                                PreviousUsage = lastAction.BufferUsage,
                                NextUsage = action.BufferUsage,
                                PreviousOperation = lastAction.Operation,
                                NextOperation = action.Operation,
                                ResourceId = resourceId,
                                PassId = action.PassId
                            };

                            if (!syncGroups.ContainsKey(action.PassId)) syncGroups[action.PassId] = [];
                            syncGroups[action.PassId].Add(sync);
                        }
                }
                else
                {
                    if (lastAction == null && action.ImageLayout != ImageLayout.Undefined)
                    {
                        var sync = new ImageResourceSync
                        {
                            PreviousLayout = ImageLayout.Undefined,
                            NextLayout = action.ImageLayout,
                            PreviousOperation = ResourceOperation.Write,
                            NextOperation = action.Operation,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        };

                        if (!syncGroups.ContainsKey(action.PassId)) syncGroups[action.PassId] = [];
                        syncGroups[action.PassId].Add(sync);
                    }
                    else if (lastAction != null && (action.Operation == ResourceOperation.Write ||
                                                    action.ImageLayout != lastAction.ImageLayout ||
                                                    action.Operation != lastAction.Operation))
                    {
                        var sync = new ImageResourceSync
                        {
                            PreviousLayout = lastAction.ImageLayout,
                            NextLayout = action.ImageLayout,
                            PreviousOperation = lastAction.Operation,
                            NextOperation = action.Operation,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        };

                        if (!syncGroups.ContainsKey(action.PassId)) syncGroups[action.PassId] = [];
                        syncGroups[action.PassId].Add(sync);
                    }
                }

                lastAction = action;
            }
        }

        var executionLevels = new Dictionary<uint, int>();

        var queue = new Queue<IPass>();
        var passDependenciesCount = new Dictionary<uint, int>(nodes.Count);
        foreach (var (passId, dependencies) in nodes)
        {
            passDependenciesCount[passId] = dependencies.Count;
            if (dependencies.Count == 0)
            {
                var pass = _passes[passId];
                queue.Enqueue(pass);
                executionLevels[pass.Id] = 0;
            }
        }
        var maxLevel = 0;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var passId = current.Id;
            var currentLevel = executionLevels[passId];

            if (passParents.TryGetValue(passId, out var parents))
                foreach (var parentId in parents)
                {
                    passDependenciesCount[parentId]--;

                    // Ensure dependent is scheduled after its deepest dependency
                    var newLevel = currentLevel + 1;
                    if (!executionLevels.TryGetValue(parentId, out var existingLevel) || newLevel > existingLevel)
                    {
                        executionLevels[parentId] = newLevel;
                        if (newLevel > maxLevel) maxLevel = newLevel;
                    }

                    if (passDependenciesCount[parentId] == 0)
                        queue.Enqueue(_passes[parentId]);
                }
        }

        var executionGroups = new List<IPass>[maxLevel + 1];

        for (var i = 0; i < executionGroups.Length; i++) executionGroups[i] = [];

        foreach (var (pass, level) in executionLevels) executionGroups[level].Add(_passes[pass]);

        var finalExecutionGroups = new List<ExecutionGroup>();

        foreach (var group in executionGroups)
        {
            var bufferSyncs = new List<BufferResourceSync>();
            var imageSyncs = new List<ImageResourceSync>();

            foreach (var pass in group)
            {
                if (!syncGroups.TryGetValue(pass.Id, out var syncGroup)) continue;
                foreach (var sync in syncGroup)
                    switch (sync)
                    {
                        case BufferResourceSync asBufferSync:
                            bufferSyncs.Add(asBufferSync);
                            break;
                        case ImageResourceSync asImageSync:
                            imageSyncs.Add(asImageSync);
                            break;
                    }
            }

            if (imageSyncs.NotEmpty() || bufferSyncs.NotEmpty())
                finalExecutionGroups.Add(new ExecutionGroup
                {
                    Passes =
                    [
                        new BarrierPass(bufferSyncs.ToArray(), imageSyncs.ToArray())
                        {
                            Id = MakeId()
                        }
                    ]
                });

            finalExecutionGroups.Add(new ExecutionGroup
            {
                Passes = group
            });
        }


        var finalResources = new Dictionary<uint, IResourceDescriptor>(finalResourceActions.Count);
        foreach (var id in finalResourceActions.Keys) finalResources[id] = config.Resources[id];

        return new CompiledGraph(resourcePool, frame,
            finalResources,
            finalExecutionGroups);
    }

    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null)
    {
        var id = MakeId();
        _externalResources.Add(id, ExternalResourceDescriptors.Make(handle, onDispose));
        return id;
    }

    public uint AddDestinationImage(ResourceHandle handle, Action? onDispose = null)
    {
        return _swapchainImageId = AddExternalImage(handle, onDispose);
    }

    public void Reset()
    {
        // _images.Clear();
        // _memory.Clear();
        _passes.Clear();
        _externalResources.Clear();
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

    private GraphConfig Configure()
    {
        Debug.Assert(_swapchainImageId != 0, "A swapchain image must be added to the graph");

        var config = new GraphConfig(this)
        {
            SwapchainImageId = _swapchainImageId
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
}