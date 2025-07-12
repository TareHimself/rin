using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.PassConfigs;

public class BlurPassConfig : IPassConfig
{
    [PublicAPI] public uint MainImageId => PassContext.MainImageId;
    
    [PublicAPI] public uint CopyImageId => PassContext.CopyImageId;
    [PublicAPI] public uint StencilImageId => PassContext.StencilImageId;

    public SurfacePassContext PassContext { get; init; } = null!;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(CopyImageId, ImageLayout.ShaderReadOnly);
        config.ReadImage(StencilImageId, ImageLayout.StencilAttachment);
    }

    public void Begin(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetImageOrException(MainImageId);
        var stencilImage = graph.GetImageOrException(StencilImageId);
        
        ctx.BeginRendering(PassContext.Extent, [drawImage], stencilAttachment: stencilImage)
            .DisableFaceCulling()
            .StencilCompareOnly();
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx.EndRendering();
    }
}