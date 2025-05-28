using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.World.Graphics;

public class InitWorldPass : IPass
{
    private readonly WorldContext _worldContext;
    
    public InitWorldPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    public void PreAdd(IGraphBuilder builder)
    {
        
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }


    public void Configure(IGraphConfig config)
    {
        _worldContext.Init(Id);
        _worldContext.GBufferImage0 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _worldContext.GBufferImage1 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _worldContext.GBufferImage2 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _worldContext.DepthImageId = 
            config.CreateImage(_worldContext.Extent, ImageFormat.Depth, ImageLayout.General);
    }
    
    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_worldContext.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_worldContext.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_worldContext.GBufferImage2);
        var depthImage = graph.GetImageOrException(_worldContext.DepthImageId);
        ctx
            .ClearColorImages(Vector4.Zero,ImageLayout.General,gBuffer0, gBuffer1, gBuffer2)
            .ClearDepthImages(0,ImageLayout.General,depthImage);
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;
    
    public void Dispose()
    {
    }
}