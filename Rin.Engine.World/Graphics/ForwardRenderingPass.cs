using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.World.Components;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class ForwardRenderingPass(CameraComponent camera, Extent2D size, CollectPass? collectPass = null)
    : IPass
{
    private readonly CollectPass _collectPass = collectPass ?? new CollectPass(camera, size);
    private readonly bool _ownCollectPass = collectPass == null;
    private Extent2D _size = size;

    [PublicAPI] public uint OutputImageId { get; private set; }

    [PublicAPI] public IGraphImage? OutputImage { get; set; }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }

    [PublicAPI] public uint SceneBufferId { get; set; }

    [PublicAPI] public uint LightsBufferId { get; set; }

    [PublicAPI] public uint MaterialBufferId { get; set; }

    public void PreAdd(IGraphBuilder builder)
    {
        if (_ownCollectPass) builder.AddPass(_collectPass);
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        LightsBufferId = _collectPass.Lights.Length > 0
            ? config.CreateBuffer<LightInfo>(_collectPass.Lights.Length, BufferStage.Graphics)
            : 0;
        SceneBufferId = config.CreateBuffer<SceneInfo>(BufferStage.Graphics);
        var materialBufferSize = _collectPass.ProcessedGeometry.Aggregate((ulong)0,
            (total, geometryDrawCommand) => total + geometryDrawCommand.Material.ColorPass.GetRequiredMemory());
        MaterialBufferId = materialBufferSize > 0 ? config.CreateBuffer(materialBufferSize, BufferStage.Graphics) : 0;
        var (width, height) = _size;
        OutputImageId = config.CreateImage(width, height, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        DepthImageId = config.ReadImage(_collectPass.DepthImageId, ImageLayout.DepthAttachment);

        if (_collectPass.SkinningOutputBufferId > 0)
            config.ReadBuffer(_collectPass.SkinningOutputBufferId, BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var cmd = frame.GetCommandBuffer();
        var materialBuffer = MaterialBufferId > 0 ? graph.GetBuffer(MaterialBufferId) : null;
        var sceneBuffer = graph.GetBuffer(SceneBufferId);
        var lightsBuffer = LightsBufferId > 0 ? graph.GetBuffer(LightsBufferId) : null;

        ulong materialBufferSize = 0;

        OutputImage = graph.GetImage(OutputImageId);
        DepthImage = graph.GetImage(DepthImageId);
        cmd
            .BeginRendering(_size.ToVk(), [OutputImage.MakeColorAttachmentInfo(new Vector4(0.0f))],
                DepthImage.MakeDepthAttachmentInfo())
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_BACK_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(false, VkCompareOp.VK_COMPARE_OP_GREATER_OR_EQUAL)
            .DisableBlending(0, 1)
            .SetVertexInput([], [])
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = _size.Width,
                    height = _size.Height,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = _size.ToVk()
                }
            ]);

        var sceneFrame = new SceneFrame(frame, _collectPass.View, _collectPass.Projection, sceneBuffer);
        lightsBuffer?.Write(_collectPass.Lights);
        var lightsAddress = lightsBuffer?.GetAddress() ?? 0;
        sceneBuffer.Write(new SceneInfo
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection,
            CameraPosition = _collectPass.CameraTransform.Position,
            NumLights = _collectPass.Lights.Length,
            LightsAddress = lightsAddress
        });


        foreach (var geometryInfos in _collectPass.ProcessedGeometry.GroupBy(c => c,
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

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;
    public bool HandlesPreAdd => true;
    public bool HandlesPostAdd => false;

    public void Dispose()
    {
    }
}