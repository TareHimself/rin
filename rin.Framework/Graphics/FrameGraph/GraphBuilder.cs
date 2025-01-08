using System.Collections.Frozen;
using rin.Framework.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    
    
    
    // private readonly Dictionary<uint, ImageResourceDescriptor> _images = [];
    // private readonly Dictionary<uint, MemoryResourceDescriptor> _memory = [];
    
    private readonly Dictionary<uint, IPass> _passes = [];
    private uint _id;
    private readonly object _lock = new object();
    
    /// <summary>
    /// All Resource ID's must be greater than zero
    /// </summary>
    /// <returns></returns>
    public uint MakeId()
    {
        lock (_lock)
        {
            return ++_id;
        }
    }
    public uint AddPass(IPass pass)
    {
        var passId = MakeId();
        pass.Id = passId;
        _passes.Add(passId,pass);
        return passId;
    }
    
    private GraphConfig Configure()
    {
        var config = new GraphConfig(this);
        foreach (var (id,pass) in _passes)
        {
            config.CurrentPassId = id;
            pass.Configure(config);
        }
        return config;
    }
    
    public ICompiledGraph? Compile(IImagePool imagePool,Frame frame)
    {
        var config = Configure();
        
        var terminals = _passes.Values.Where(p => p.IsTerminal).ToArray();
        
        if (terminals.Empty())
        {
            return null;
        }
        
        HashSet<uint> visited = [];
        LinkedList<ICompiledGraphNode> nodes = [];
        HashSet<uint> resources = [];
        var toCheck = terminals.Select(c => c.Id).ToQueue();

        while (toCheck.NotEmpty())
        {
            var passId = toCheck.Dequeue();
            
            if(visited.Contains(passId)) continue;

            var node = new CompiledGraphNode()
            {
                Pass = _passes[passId],
                Dependencies = []
            };
            
            foreach (var dependency in config.PassDependencies[passId])
            {
                switch (dependency.Type)
                {
                    case GraphConfig.DependencyType.Pass:
                    {
                        toCheck.Enqueue(dependency.Id);
                        node.Dependencies.Add(_passes[dependency.Id]);
                    }
                        break;
                    case GraphConfig.DependencyType.Read:
                    {
                        var resourceActions = config.ResourceActions[dependency.Id];
                        
                        // Find the position of our read
                        var targetIdx = resourceActions.FindLastIndex((c) => c.PassId == passId && c.Type == GraphConfig.ActionType.Read);
                        
                        // If found find the position of the write before it
                        if (targetIdx != -1)
                        {
                            GraphConfig.ResourceAction? targetAction = null;
                            for (var i = targetIdx; i > -1; i--)
                            {
                                if (resourceActions[i].Type == GraphConfig.ActionType.Write)
                                {
                                    targetAction = resourceActions[i];
                                    break;
                                }
                            }

                            // If found add the write as a dependency and to the search
                            if (targetAction != null)
                            {
                                toCheck.Enqueue(targetAction.PassId);
                                node.Dependencies.Add(_passes[targetAction.PassId]);
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
                            var targetIdx = resourceActions.FindLastIndex((c) => c.PassId == passId && c.Type == GraphConfig.ActionType.Write);
                        
                            // If found find the position of the write before it
                            if (targetIdx != -1)
                            {
                                GraphConfig.ResourceAction? targetAction = null;
                                for (var i = targetIdx; i > -1; i--)
                                {
                                    if (resourceActions[i].Type == GraphConfig.ActionType.Write)
                                    {
                                        targetAction = resourceActions[i];
                                        break;
                                    }
                                }

                                // If found add the write as a dependency and to the search
                                if (targetAction != null)
                                {
                                    toCheck.Enqueue(targetAction.PassId);
                                    node.Dependencies.Add(_passes[targetAction.PassId]);
                                    resources.Add(dependency.Id);
                                }
                            }
                        }
                        else
                        {
                            resources.Add(dependency.Id);
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            nodes.AddFirst(node);
            
            visited.Add(passId);
        }
        
        return new CompiledGraph(imagePool,frame,resources.ToDictionary((id) => id,(id) => config.Resources[id]), nodes.ToArray());
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
}