using Rin.Framework.Graphics.Graph;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Window;

public class WindowSurfaceCollectedData(CommandList commandList) : DefaultCollectedSurfaceData(commandList)
{
    public override void Write(IGraphBuilder builder)
    {
        base.Write(builder);
        builder.AddPass(new CopySurfaceToSwapchain(SurfaceContext));
    }
}