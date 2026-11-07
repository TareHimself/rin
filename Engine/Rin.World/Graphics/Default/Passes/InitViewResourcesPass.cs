using System.Numerics;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

public class InitViewResourcesPass(DefaultWorldViewData view) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        view.OutputImageId = config.CreateTexture(view.Extent, PbrShader.Descriptor.Output.Format.ToImageFormat(), ImageLayout.ShaderAccess);
        view.DepthImageId = config.CreateTexture(view.Extent, ImageFormat.Depth, ImageLayout.ShaderAccess);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx
            .ClearColorImages(new Vector4(0f, 0f, 0f, 1f), [graph.GetImageOrException(view.OutputImageId)])
            .ClearDepthImages(0, [graph.GetImageOrException(view.DepthImageId)]);
    }
}
