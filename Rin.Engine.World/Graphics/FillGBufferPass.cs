using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class FillGBufferPass : IPass
{
    private readonly WorldContext _worldContext;
    private uint _materialBufferId;
    private uint _worldBufferId;

    public FillGBufferPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(_worldContext.DepthImageId, ImageLayout.DepthAttachment);
        _worldContext.GBufferImage0 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        _worldContext.GBufferImage1 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        _worldContext.GBufferImage2 =
            config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        if (_worldContext.SkinningOutputBufferId > 0)
            config.ReadBuffer(_worldContext.SkinningOutputBufferId, BufferStage.Graphics);

        var materialBufferSize = _worldContext.ProcessedMeshes.Aggregate((ulong)0,
            (total, geometryDrawCommand) => total + geometryDrawCommand.Material.ColorPass.GetRequiredMemory());
        if (materialBufferSize > 0) _materialBufferId = config.CreateBuffer(materialBufferSize, BufferStage.Graphics);

        _worldBufferId = config.CreateBuffer<WorldInfo>(BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cmd = ctx.GetCommandBuffer();
        var gBuffer0 = graph.GetImageOrException(_worldContext.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_worldContext.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_worldContext.GBufferImage2);
        var depthImage = graph.GetImageOrException(_worldContext.DepthImageId);

        var materialBuffer = graph.GetBufferOrNull(_materialBufferId);
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);

        ulong materialBufferSize = 0;
        var extent = _worldContext.Extent;
        cmd
            .BeginRendering(extent, [
                    gBuffer0.MakeColorAttachmentInfo(new Vector4(0.0f)),
                    gBuffer1.MakeColorAttachmentInfo(new Vector4(0.0f)),
                    gBuffer2.MakeColorAttachmentInfo(new Vector4(0.0f))
                ],
                depthImage.MakeDepthAttachmentInfo())
            .DisableStencilTest()
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_BACK_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(false)
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = extent.Width,
                    height = extent.Height,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = extent.ToVk()
                }
            ]);

        var sceneFrame = new WorldFrame(_worldContext.View, _worldContext.Projection, worldBuffer, cmd);

        worldBuffer.Write(new WorldInfo
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection,
            CameraPosition = _worldContext.ViewTransform.Position
        });

        foreach (var geometryInfos in _worldContext.ProcessedMeshes.GroupBy(c => c,
                     new ProcessedMesh.CompareByIndexAndMaterial()))
        {
            var infos = geometryInfos.ToArray();
            var first = infos.First();
            var size = first.Material.ColorPass.GetRequiredMemory() * (ulong)infos.Length;
            var view = materialBuffer?.GetView(materialBufferSize, size);
            materialBufferSize += size;
            first.Material.ColorPass.Execute(sceneFrame, view, infos);
        }

        cmd.EndRendering();
    }
}