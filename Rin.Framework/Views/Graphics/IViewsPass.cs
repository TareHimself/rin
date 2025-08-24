using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Views.Graphics;

public interface IViewsPass
{
    public static abstract IViewsPass Create(PassCreateInfo info);

    public void Configure(IGraphConfig config);
    public void Begin(ICompiledGraph graph, IExecutionContext ctx);
    public void End(ICompiledGraph graph, IExecutionContext ctx);
}