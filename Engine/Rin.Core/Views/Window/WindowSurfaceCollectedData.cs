using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Window;

public class WindowSurfaceCollectedData(CommandList commandList) : DefaultCollectedSurfaceData(commandList)
{
    public override void Write(IGraphBuilder builder)
    {
        base.Write(builder);
        builder.AddPass(new CopySurfaceToSwapchain(SurfaceContext));
    }
}