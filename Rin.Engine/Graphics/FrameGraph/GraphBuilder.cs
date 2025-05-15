using System.Diagnostics;
using Rin.Engine.Extensions;

namespace Rin.Engine.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<uint, ExternalImageResourceDescriptor> _externalImages = [];
    private readonly object _lock = new();
    private readonly Dictionary<uint, IPass> _passes = [];
    private uint _id;
    private uint _swapchainImageId;

    public uint AddPass(IPass pass)
    {
        if (pass.HandlesPreAdd)
            pass.PreAdd(this);

        var passId = MakeId();
        pass.Id = passId;
        _passes.Add(passId, pass);

        if (pass.HandlesPostAdd)
            pass.PostAdd(this);
        return passId;
    }

    public ICompiledGraph? Compile(IResourcePool resourcePool, Frame frame)
    {
        var config = Configure();

        var terminals = _passes.Values.Where(p => p.IsTerminal).ToArray();

        if (terminals.Empty()) return null;

        // Nodes and their dependencies
        Dictionary<uint, HashSet<uint>> nodes = [];
        var toCheck = terminals.Select(c => c.Id).ToQueue();
        var visited = toCheck.ToHashSet();

        // number of passes that depend on the key
        var passParents = new Dictionary<uint, HashSet<uint>>();

        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            HashSet<uint> dependencies = [];
            nodes[passId] = dependencies;
            foreach (var dependency in config.PassDependencies[passId])
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
                            c.PassId == passId && c.Usage == ResourceUsage.Read);

                        // If found find the position of the write before it
                        if (targetIdx != -1)
                        {
                            GraphConfig.ResourceAction? targetAction = null;
                            for (var i = targetIdx - 1; i > -1; i--)
                                if (resourceActions[i].Usage == ResourceUsage.Write)
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
                                c.PassId == passId && c.Usage == ResourceUsage.Write);

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

                                    if (resourceActions[i].Usage == ResourceUsage.Write) break;
                                }
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        //var finalPassIds = nodes.Keys.ToHashSet();
        var finalResourceActions = config.ResourceActions.ToDictionary(c => c.Key, c =>
        {
            c.Value.RemoveAll(a => !nodes.ContainsKey(a.PassId));
            return c.Value;
        });

        var syncGroups = new Dictionary<uint, List<PassResourceSync>>();
        // Sync just before use (could change to batch syncing)
        foreach (var (resourceId, actions) in finalResourceActions)
        {
            if (actions.Empty()) continue;
            GraphConfig.ResourceAction? lastAction = null;
            foreach (var action in actions)
            {
                if (action.Type == ResourceType.Buffer)
                {
                    if (lastAction != null)
                        if (action.Usage != lastAction.Usage || action.Usage == ResourceUsage.Write)
                        {
                            var sync = new BufferResourceSync
                            {
                                PreviousStage = lastAction.BufferStage,
                                NextStage = action.BufferStage,
                                PreviousUsage = lastAction.Usage,
                                NextUsage = action.Usage,
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
                            PreviousUsage = ResourceUsage.Write,
                            NextUsage = action.Usage,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        };

                        if (!syncGroups.ContainsKey(action.PassId)) syncGroups[action.PassId] = [];
                        syncGroups[action.PassId].Add(sync);
                    }
                    else if (lastAction != null && (action.Usage == ResourceUsage.Write ||
                                                    action.ImageLayout != lastAction.ImageLayout ||
                                                    action.Usage != lastAction.Usage))
                    {
                        var sync = new ImageResourceSync
                        {
                            PreviousLayout = lastAction.ImageLayout,
                            NextLayout = action.ImageLayout,
                            PreviousUsage = lastAction.Usage,
                            NextUsage = action.Usage,
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

        var queue = new Queue<IPass>(
            nodes.Where(kv => kv.Value.Empty()).Select(c => _passes[c.Key])
        );

        foreach (var pass in queue)
            executionLevels[pass.Id] = 0;

        var passDependenciesCount = nodes.ToDictionary(c => c.Key, c => c.Value.Count);
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
                        executionLevels[parentId] = newLevel;

                    if (passDependenciesCount[parentId] == 0)
                        queue.Enqueue(_passes[parentId]);
                }
        }


        var maxLevel = executionLevels.Values.Max();
        var executionGroups = new List<IPass>[maxLevel + 1];

        for (var i = 0; i < executionGroups.Length; i++) executionGroups[i] = [];

        foreach (var (pass, level) in executionLevels) executionGroups[level].Add(_passes[pass]);

        var finalExecutionGroups = new List<ExecutionGroup>();

        foreach (var group in executionGroups)
        {
            var bufferSyncs = new List<BufferResourceSync>();
            var imageSyncs = new List<ImageResourceSync>();

            foreach (var sync in group.SelectMany(c =>
                         syncGroups.TryGetValue(c.Id, out var syncGroup) ? syncGroup : []))
                switch (sync)
                {
                    case BufferResourceSync asBufferSync:
                        bufferSyncs.Add(asBufferSync);
                        break;
                    case ImageResourceSync asImageSync:
                        imageSyncs.Add(asImageSync);
                        break;
                }

            if (imageSyncs.NotEmpty() || bufferSyncs.NotEmpty())
                finalExecutionGroups.Add(new ExecutionGroup
                {
                    Passes =
                    [
                        new BarrierPass(bufferSyncs, imageSyncs)
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


        return new CompiledGraph(resourcePool, frame,
            finalResourceActions.Keys.ToDictionary(id => id, id => config.Resources[id]),
            finalExecutionGroups);
    }

    public uint AddExternalImage(IDeviceImage image, Action? onDispose)
    {
        var id = MakeId();
        _externalImages.Add(id, new ExternalImageResourceDescriptor(image, onDispose));
        return id;
    }

    public uint AddSwapchainImage(IDeviceImage image, Action? onDispose)
    {
        return _swapchainImageId = AddExternalImage(image, onDispose);
    }

    public void Reset()
    {
        // _images.Clear();
        // _memory.Clear();
        _passes.Clear();
        lock (_lock)
        {
            _id = 0;
        }
    }

    /// <summary>
    ///     All Resource ID's must be greater than zero
    /// </summary>
    /// <returns></returns>
    public uint MakeId()
    {
        lock (_lock)
        {
            return ++_id;
        }
    }

    private GraphConfig Configure()
    {
        Debug.Assert(_swapchainImageId != 0, "A swapchain image must be added to the graph");

        var config = new GraphConfig(this)
        {
            SwapchainImageId = _swapchainImageId
        };

        foreach (var (id, externalImageResourceDescriptor) in _externalImages)
            config.Resources.Add(id, externalImageResourceDescriptor);

        foreach (var (id, pass) in _passes)
        {
            config.CurrentPassId = id;
            pass.Configure(config);
        }
        
        config.FillImageResources();

        return config;
    }
}