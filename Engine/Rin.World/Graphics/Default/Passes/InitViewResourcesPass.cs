using System.Numerics;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

public class InitViewResourcesPass(DefaultWorldViewData view) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        view.GBufferImage0 = config.CreateTexture(view.Extent, MeshShader.Descriptor.Output.GBuffer0Format.ToImageFormat(), ImageLayout.ShaderAccess);
        view.GBufferImage1 = config.CreateTexture(view.Extent, MeshShader.Descriptor.Output.GBuffer1Format.ToImageFormat(), ImageLayout.ShaderAccess);
        view.GBufferImage2 = config.CreateTexture(view.Extent, MeshShader.Descriptor.Output.GBuffer2Format.ToImageFormat(), ImageLayout.ShaderAccess);
        view.GBufferImage3 = config.CreateTexture(view.Extent, MeshShader.Descriptor.Output.GBuffer3Format.ToImageFormat(), ImageLayout.ShaderAccess);
        view.DepthImageId = config.CreateTexture(view.Extent, ImageFormat.Depth, ImageLayout.ShaderAccess);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx
            .ClearColorImages(Vector4.Zero, [
                graph.GetImageOrException(view.GBufferImage0),
                graph.GetImageOrException(view.GBufferImage1),
                graph.GetImageOrException(view.GBufferImage2),
                graph.GetImageOrException(view.GBufferImage3)
            ])
            .ClearDepthImages(0, [graph.GetImageOrException(view.DepthImageId)]);
    }
}
