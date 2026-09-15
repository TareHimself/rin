using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics;

public interface IPassConfig
{
    public void Init(SurfaceContext surfaceContext);
    public void Configure(IGraphConfig config);
    public void Begin(ICompiledGraph graph, IExecutionContext ctx);
    public void End(ICompiledGraph graph, IExecutionContext ctx);
}