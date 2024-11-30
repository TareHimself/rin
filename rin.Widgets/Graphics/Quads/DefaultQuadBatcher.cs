using System.Runtime.InteropServices;
using rin.Core;
using rin.Graphics;
using rin.Graphics.Descriptors;
using rin.Graphics.Shaders;
using rin.Core.Math;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Widgets.Graphics.Quads;

public sealed class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private partial struct Push
    {
        public Matrix4 Projection;
        public ulong Buffer;
    }
    
    private readonly GraphicsShader _batchShader = GraphicsShader.FromFile(Path.Join(SRuntime.ResourcesDirectory, "shaders", "widgets", "batch.rsl"));
    
    protected override GraphicsShader GetShader() => _batchShader;

    protected override QuadBatch MakeNewBatch() => new QuadBatch();

    protected override uint WriteBatch(WidgetFrame frame, IDeviceBuffer view, QuadBatch batch, GraphicsShader shader)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        var resourceSet = SGraphicsModule.Get().GetResourceManager().GetDescriptorSet();
        var quads = batch.GetQuads().ToArray();
        view.Write(quads);
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            _batchShader.GetPipelineLayout(), new DescriptorSet[] {resourceSet});
            
        var pushResource = _batchShader.PushConstants.First().Value;
        var push = new Push()
        {
            Projection = frame.Projection,
            Buffer = view.GetAddress()
                    
        };
        cmd.PushConstant(_batchShader.GetPipelineLayout(), pushResource.Stages,push);
        return (uint)quads.Length;
    }
}