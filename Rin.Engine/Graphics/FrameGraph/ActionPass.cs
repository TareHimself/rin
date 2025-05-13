using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class ActionPass(
    Action<IPass, IGraphConfig> configure,
    Action<IPass, ICompiledGraph, Frame, IRenderContext> run,
    bool terminal = false,
    string? name = null) : IPass
{
    public string Name { get; } = name ?? $"unknown-pass-{Guid.NewGuid().ToString()}";

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        configure(this, config);
    }

    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
        run(this, graph, frame, context);
    }

    public uint Id { get; set; }

    public bool IsTerminal => terminal;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void Dispose()
    {
    }
}