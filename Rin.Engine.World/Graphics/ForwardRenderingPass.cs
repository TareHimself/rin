using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class ForwardRenderingPass(CameraComponent camera, Vector2<uint> size, CollectScenePass? collectPass = null)
    : IPass
{
    private readonly CollectScenePass _collectPass = collectPass ?? new CollectScenePass(camera, size);
    private readonly bool _ownCollectPass = collectPass == null;
    private Vector2<uint> _size = size;

    [PublicAPI] public uint OutputImageId { get; private set; }

    [PublicAPI] public IGraphImage? OutputImage { get; set; }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }

    [PublicAPI] public uint SceneBufferId { get; set; }

    [PublicAPI] public uint LightsBufferId { get; set; }

    [PublicAPI] public uint MaterialBufferId { get; set; }

    public void Added(IGraphBuilder builder)
    {
        if (_ownCollectPass) builder.AddPass(_collectPass);
    }

    public void Configure(IGraphConfig config)
    {
        LightsBufferId = _collectPass.Lights.Length > 0
            ? config.AllocateBuffer<LightInfo>(_collectPass.Lights.Length)
            : 0;
        SceneBufferId = config.AllocateBuffer<SceneInfo>();
        var materialBufferSize = _collectPass.Geometry.Aggregate((ulong)0,
            (total, geometryDrawCommand) => total + geometryDrawCommand.Material.ColorPass.GetRequiredMemory());
        MaterialBufferId = materialBufferSize > 0 ? config.AllocateBuffer(materialBufferSize) : 0;
        var (width, height) = _size;
        OutputImageId = config.CreateImage(width, height, ImageFormat.RGBA32);
        DepthImageId = config.Read(_collectPass.DepthImageId);
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
            .ImageBarrier(OutputImage, ImageLayout.General)
            .ClearColorImages(new Vector4(0.0f), ImageLayout.General, OutputImage)
            .ImageBarrier(OutputImage, ImageLayout.General, ImageLayout.ColorAttachment)
            .BeginRendering(_size.ToVkExtent(), [OutputImage.MakeColorAttachmentInfo(new Vector4(0.0f))],
                DepthImage.MakeDepthAttachmentInfo())
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(false,VkCompareOp.VK_COMPARE_OP_GREATER_OR_EQUAL)
            .DisableBlending(0, 1)
            .SetVertexInput([], [])
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = _size.X,
                    height = _size.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D
                    {
                        width = _size.X,
                        height = _size.Y
                    }
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
            CameraPosition = _collectPass.CameraTransform.Location,
            NumLights = _collectPass.Lights.Length,
            LightsAddress = lightsAddress
        });


        foreach (var geometryInfos in _collectPass.OpaqueGeometry.GroupBy(c => new
                 {
                     Type = c.Material.GetType(),
                     c.Mesh
                 }))
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

    public void Dispose()
    {
    }
}