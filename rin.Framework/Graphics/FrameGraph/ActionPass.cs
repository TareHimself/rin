using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class ActionPass(Action<IPass,IGraphConfig> configure,Action<IPass,ICompiledGraph,Frame,VkCommandBuffer> run,bool terminal = false,string? name = null) : IPass
{
    public void BeforeAdd(IGraphBuilder builder)
    {
        
    }

    public void Configure(IGraphConfig config)
    {
        configure(this,config);
    }

    public void Execute(ICompiledGraph graph,Frame frame, VkCommandBuffer cmd)
    {
        run(this,graph,frame, cmd);
    }

    public uint Id { get; set; }

    public string Name { get; } = name ?? $"unknown-pass-{Guid.NewGuid().ToString()}";

    public bool IsTerminal => terminal;

    public void Dispose()
    {
        
    }
}