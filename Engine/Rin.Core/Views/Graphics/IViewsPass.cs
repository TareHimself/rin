using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics;

public interface IViewsPass
{
    public static abstract IViewsPass Create(PassCreateInfo info);

    public void Configure(IGraphConfig config);
    public void Begin(ICompiledGraph graph, IExecutionContext ctx);
    public void End(ICompiledGraph graph, IExecutionContext ctx);
}