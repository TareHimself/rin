using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class ActionPass(Action<IPass,IGraphBuilder> configure,Action<IPass,ICompiledGraph,Frame,VkCommandBuffer> run,bool terminal = false,string? name = null) : IPass
{
    public void Configure(IGraphBuilder builder)
    {
        configure(this,builder);
    }

    public void Execute(ICompiledGraph graph,Frame frame, VkCommandBuffer cmd)
    {
        run(this,graph,frame, cmd);
    }

    public string Name { get; } = name ?? $"unknown-pass-{Guid.NewGuid().ToString()}";

    public bool IsTerminal => terminal;

    public void Dispose()
    {
        
    }
}