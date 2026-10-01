using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default;

public class ShadowPass(DefaultWorldViewData collectedData) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}