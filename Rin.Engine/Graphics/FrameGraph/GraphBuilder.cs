using Rin.Engine.Extensions;

namespace Rin.Engine.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<uint, IGraphImage> _externalImages = [];
    private readonly object _lock = new();
    private readonly Dictionary<uint, IPass> _passes = [];
    private uint _id;

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

        Dictionary<uint, ICompiledGraphNode> nodes = [];
        var toCheck = terminals.Select(c => c.Id).ToQueue();
        var visited = toCheck.ToHashSet();

        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            var node = new CompiledGraphNode
            {
                Pass = _passes[passId],
                Dependencies = []
            };

            foreach (var dependency in config.PassDependencies[passId])
                switch (dependency.Type)
                {
                    case GraphConfig.DependencyType.Pass:
                    {
                        if (!visited.Add(dependency.Id)) continue;
                        toCheck.Enqueue(dependency.Id);
                        node.Dependencies.Add(_passes[dependency.Id]);
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
                                node.Dependencies.Add(_passes[action.PassId]);
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
                            {
                                List<GraphConfig.ResourceAction> actions = [];
                                for (var i = targetIdx - 1; i > -1; i--)
                                {
                                    if (resourceActions[i].Usage == ResourceUsage.Write)
                                    {
                                        actions.Add(resourceActions[i]);
                                        break;
                                    }

                                    actions.Add(resourceActions[i]);
                                }

                                // Write can't happen till all reads since last write happen
                                foreach (var action in actions)
                                {
                                    node.Dependencies.Add(_passes[action.PassId]);

                                    if (!visited.Add(action.PassId)) continue;
                                    toCheck.Enqueue(action.PassId);
                                }
                            }
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            nodes.Add(passId, node);
        }

        //var finalPassIds = nodes.Keys.ToHashSet();
        var finalResourceActions = config.ResourceActions.ToDictionary(c => c.Key, c =>
        {
            c.Value.RemoveAll(a => !nodes.ContainsKey(a.PassId));
            return c.Value;
        });

        var syncPoints = new List<PassResourceSync>();
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
                            syncPoints.Add(new BufferResourceSync
                            {
                                PreviousStage = lastAction.BufferStage,
                                NextStage = action.BufferStage,
                                PreviousUsage = lastAction.Usage,
                                NextUsage = action.Usage,
                                ResourceId = resourceId,
                                PassId = action.PassId
                            });
                }
                else
                {
                    if (lastAction == null && action.ImageLayout != ImageLayout.Undefined)
                        syncPoints.Add(new ImageResourceSync
                        {
                            PreviousLayout = ImageLayout.Undefined,
                            NextLayout = action.ImageLayout,
                            PreviousUsage = ResourceUsage.Write,
                            NextUsage = action.Usage,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        });
                    else if (lastAction != null && (action.Usage == ResourceUsage.Write ||
                                                    action.ImageLayout != lastAction.ImageLayout ||
                                                    action.Usage != lastAction.Usage))
                        syncPoints.Add(new ImageResourceSync
                        {
                            PreviousLayout = lastAction.ImageLayout,
                            NextLayout = action.ImageLayout,
                            PreviousUsage = lastAction.Usage,
                            NextUsage = action.Usage,
                            ResourceId = resourceId,
                            PassId = action.PassId
                        });
                }

                lastAction = action;
            }
        }

        var syncGroups = new Dictionary<uint, List<PassResourceSync>>();

        foreach (var resource in syncPoints)
            if (syncGroups.TryGetValue(resource.PassId, out var arr))
                arr.Add(resource);
            else
                syncGroups.Add(resource.PassId, [resource]);

        foreach (var (passId, syncs) in syncGroups)
        {
            var bufferSyncs = new List<BufferResourceSync>();
            var imageSyncs = new List<ImageResourceSync>();

            foreach (var sync in syncs)
                switch (sync)
                {
                    case BufferResourceSync asBufferSync:
                        bufferSyncs.Add(asBufferSync);
                        break;
                    case ImageResourceSync asImageSync:
                        imageSyncs.Add(asImageSync);
                        break;
                }

            var targetPass = nodes[passId];

            var barrierPass = new BarrierPass(bufferSyncs, imageSyncs)
            {
                Id = MakeId()
            };
            var newNode = new CompiledGraphNode
            {
                Pass = barrierPass,
                Dependencies = targetPass.Dependencies
            };

            nodes[passId] = new CompiledGraphNode
            {
                Pass = targetPass.Pass,
                Dependencies = [barrierPass]
            };

            nodes[passId].Dependencies.Add(barrierPass);

            nodes.Add(barrierPass.Id, newNode);
        }

        Dictionary<IPass, int> executionLevels = [];

        var currentLevel = 0;
        var nextLevelQueue = new Queue<IPass>();
        var currentLevelQueue = terminals.ToQueue();
        while (currentLevelQueue.NotEmpty() || nextLevelQueue.NotEmpty())
        {
            if (currentLevelQueue.Empty())
            {
                currentLevelQueue = nextLevelQueue;
                nextLevelQueue = new Queue<IPass>();
                currentLevel++;
            }

            var pass = currentLevelQueue.Dequeue();
            executionLevels[pass] = currentLevel;
            foreach (var dependency in nodes[pass.Id].Dependencies) nextLevelQueue.Enqueue(dependency);
        }

        var finalOrder = executionLevels.OrderByDescending(c => c.Value).Select(c => nodes[c.Key.Id]).ToArray();

        return new CompiledGraph(resourcePool, frame,
            finalResourceActions.Keys.ToDictionary(id => id, id => config.Resources[id]),
            finalOrder);
    }

    public uint AddExternalImage(IGraphImage image)
    {
        var id = MakeId();
        _externalImages.Add(id, image);
        return id;
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
        var config = new GraphConfig(this);

        foreach (var (id, externalImage) in _externalImages)
            config.Resources.Add(id, new ExternalImageResourceDescriptor(externalImage));

        foreach (var (id, pass) in _passes)
        {
            config.CurrentPassId = id;
            pass.Configure(config);
        }

        return config;
    }

    private class PassResourceSync
    {
        public required uint ResourceId { get; set; }
        public required uint PassId { get; set; }
    }

    private class ImageResourceSync : PassResourceSync
    {
        public required ImageLayout PreviousLayout { get; set; }
        public required ImageLayout NextLayout { get; set; }
        public required ResourceUsage PreviousUsage { get; set; }
        public required ResourceUsage NextUsage { get; set; }
    }

    private class BufferResourceSync : PassResourceSync
    {
        public required BufferStage PreviousStage { get; set; }
        public required BufferStage NextStage { get; set; }
        public required ResourceUsage PreviousUsage { get; set; }
        public required ResourceUsage NextUsage { get; set; }
    }

    private class BarrierPass(IEnumerable<BufferResourceSync> buffers, IEnumerable<ImageResourceSync> images) : IPass
    {
        public uint Id { get; set; }
        public bool IsTerminal => false;
        public bool HandlesPreAdd => false;
        public bool HandlesPostAdd => false;

        public void PreAdd(IGraphBuilder builder)
        {
            throw new NotImplementedException();
        }

        public void PostAdd(IGraphBuilder builder)
        {
            throw new NotImplementedException();
        }

        public void Configure(IGraphConfig config)
        {
            throw new Exception("HOW HAVE YOU DONE THIS?");
        }

        public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
        {
            var cmd = frame.GetCommandBuffer();
            foreach (var bufferResourceSync in buffers)
            {
                var buffer = graph.GetBufferOrException(bufferResourceSync.ResourceId);
                cmd.BufferBarrier(buffer, bufferResourceSync.PreviousStage, bufferResourceSync.NextStage);
            }

            foreach (var imageResourceSync in images)
            {
                var image = graph.GetImageOrException(imageResourceSync.ResourceId);
                cmd.ImageBarrier(image, imageResourceSync.PreviousLayout, imageResourceSync.NextLayout);
            }
        }
    }
}