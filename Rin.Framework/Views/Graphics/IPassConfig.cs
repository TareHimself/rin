using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Views.Graphics;

public interface IPassConfig
{
    public void Init(SurfaceContext surfaceContext);
    public void Configure(IGraphConfig config);
    public void Begin(ICompiledGraph graph, IExecutionContext ctx);
    public void End(ICompiledGraph graph, IExecutionContext ctx);
}