using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Views.Graphics.Quads;

[ViewsBatcher]
public partial class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    private readonly IGraphicsShader _batchShader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "views", "batch.slang"));

    protected override IGraphicsShader GetShader()
    {
        return _batchShader;
    }

    protected override QuadBatch MakeNewBatch()
    {
        return new QuadBatch();
    }

    protected override uint WriteBatch(ViewsFrame frame, IDeviceBufferView view, QuadBatch batch,
        IGraphicsShader shader)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        var resourceSet = SGraphicsModule.Get().GetTextureManager().GetDescriptorSet();
        var quads = batch.GetQuads().ToArray();
        if (quads.Length == 0) return 0;

        view.Write(quads);
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            _batchShader.GetPipelineLayout(), new[] { resourceSet });

        var pushResource = _batchShader.PushConstants.First().Value;
        var push = new Push
        {
            Projection = frame.Projection,
            Viewport = new Vector4(0, 0, frame.SurfaceSize.X, frame.SurfaceSize.Y),
            Buffer = view.GetAddress()
        };
        cmd.PushConstant(_batchShader.GetPipelineLayout(), pushResource.Stages, push);
        return (uint)quads.Length;
    }

    private struct Push
    {
        public Mat4 Projection;
        public Vector4 Viewport;
        public ulong Buffer;
    }
}