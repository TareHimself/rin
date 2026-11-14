using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Pooling;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Window;

public class WindowSurfaceCollectedData(
    CommandList commandList,
    IWindowRenderer renderer,
    Recycler<CommandList>? commandLists = null)
    : DefaultCollectedSurfaceData(commandList, commandLists)
{
    public override void Write(IGraphBuilder builder)
    {
        base.Write(builder);
        builder.AddPass(new CopyToDestination(SurfaceContext, renderer));
    }
}