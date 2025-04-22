using Rin.Engine.Extensions;

namespace Rin.Engine.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<uint, IGraphImage> _externalImages = [];
    private readonly object _lock = new();


    // private readonly Dictionary<uint, ImageResourceDescriptor> _images = [];
    // private readonly Dictionary<uint, MemoryResourceDescriptor> _memory = [];

    private readonly Dictionary<uint, IPass> _passes = [];
    private uint _id;

    public uint AddPass(IPass pass)
    {
        pass.Added(this);
        var passId = MakeId();
        pass.Id = passId;
        _passes.Add(passId, pass);
        return passId;
    }

    public ICompiledGraph? Compile(IResourcePool resourcePool, Frame frame)
    {
        var config = Configure();

        var terminals = _passes.Values.Where(p => p.IsTerminal).ToArray();

        if (terminals.Empty()) return null;

        
        LinkedList<ICompiledGraphNode> nodes = [];
        HashSet<uint> resources = [];
        var toCheck = terminals.Select(c => c.Id).ToQueue();
        var visited = toCheck.ToHashSet();
        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            var node = new CompiledGraphNode
            {
                Pass = _passes[passId],
                Dependencies = [],
                MemoryRequired = 0
            };

            foreach (var dependency in config.PassDependencies[passId])
                switch (dependency.Type)
                {
                    case GraphConfig.DependencyType.Pass:
                    {
                        if(!visited.Add(dependency.Id)) continue;
                        toCheck.Enqueue(dependency.Id);
                        node.Dependencies.Add(_passes[dependency.Id]);
                    }
                        break;
                    case GraphConfig.DependencyType.Read:
                    {
                        var resourceActions = config.ResourceActions[dependency.Id];

                        // Find the position of our read
                        var targetIdx = resourceActions.FindLastIndex(c =>
                            c.PassId == passId && c.Type == GraphConfig.ActionType.Read);

                        // If found find the position of the write before it
                        if (targetIdx != -1)
                        {
                            GraphConfig.ResourceAction? targetAction = null;
                            for (var i = targetIdx - 1; i > -1; i--)
                                if (resourceActions[i].Type == GraphConfig.ActionType.Write)
                                {
                                    targetAction = resourceActions[i];
                                    break;
                                }

                            // If found add the write as a dependency and to the search
                            if (targetAction is {} action && visited.Add(action.PassId))
                            {
                                toCheck.Enqueue(action.PassId);
                                node.Dependencies.Add(_passes[action.PassId]);
                                resources.Add(dependency.Id);
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
                                c.PassId == passId && c.Type == GraphConfig.ActionType.Write);

                            // If found get all reads till the previous write
                            if (targetIdx != -1)
                            {
                                GraphConfig.ResourceAction? targetAction = null;
                                List<GraphConfig.ResourceAction> actions = [];
                                for (var i = targetIdx - 1; i > -1; i--)
                                {
                                    if (resourceActions[i].Type == GraphConfig.ActionType.Write)
                                    {
                                        actions.Add(resourceActions[i]);
                                        break;
                                    }
                                    
                                    actions.Add(resourceActions[i]);
                                }
                                
                                // Write can't happen till all reads since last write happen
                                foreach (var action in actions)
                                {
                                    if(!visited.Add(action.PassId)) continue;
                                    toCheck.Enqueue(action.PassId);
                                    node.Dependencies.Add(_passes[action.PassId]);
                                    resources.Add(dependency.Id);
                                }
                            }
                        }
                        else
                        {
                            resources.Add(dependency.Id);
                            {
                                if (config.Resources[dependency.Id] is BufferResourceDescriptor asBufferDescriptor)
                                    node.MemoryRequired += asBufferDescriptor.Size;
                            }
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            nodes.AddFirst(node);
        }

        return new CompiledGraph(resourcePool, frame, resources.ToDictionary(id => id, id => config.Resources[id]),
            nodes.ToArray());
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
}