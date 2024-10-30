using System.Runtime.InteropServices;
using rin.Core;
using rin.Graphics;
using rin.Graphics.Descriptors;
using rin.Graphics.Shaders;
using rin.Core.Math;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Widgets.Graphics.Quads;

public class QuadBatchRenderer : IBatchRenderer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private partial struct Push
    {
        public Matrix4 Projection;
        public ulong Buffer;
    }

    private readonly GraphicsShader _batchShader = GraphicsShader.FromFile(Path.Join(SRuntime.ResourcesDirectory, "shaders", "widgets", "batch.rsl"));

    public void Draw(WidgetFrame frame, IBatch batch, DeviceBuffer buffer, ulong address, ulong offset)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        if (batch is QuadBatch asQuadBatch && _batchShader.Bind(cmd, true))
        {
            var resourceSet = SGraphicsModule.Get().GetResourceManager().GetDescriptorSet();
            var quads = asQuadBatch.GetQuads().ToArray();
            var size = asQuadBatch.GetMemoryNeeded().First();
            buffer.Write(quads, offset);
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                _batchShader.GetPipelineLayout(), new DescriptorSet[] {resourceSet});
            
            var pushResource = _batchShader.PushConstants.First().Value;
            var push = new Push()
            {
                Projection = frame.Projection,
                Buffer = address
                    
            };
            cmd.PushConstant(_batchShader.GetPipelineLayout(), pushResource.Stages,push);
            
            vkCmdDraw(cmd, 6, (uint)quads.Length, 0, 0);
        }
    }

    public IBatch NewBatch() => new QuadBatch();
}