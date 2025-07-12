using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics;


public interface IPassConfig
{
    public SurfacePassContext PassContext { get; init; }
    public void Configure(IGraphConfig config);
    public void Begin(ICompiledGraph graph, IExecutionContext ctx);
    public void End(ICompiledGraph graph, IExecutionContext ctx);
}