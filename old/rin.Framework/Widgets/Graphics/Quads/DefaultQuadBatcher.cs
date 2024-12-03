using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Rsl;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Widgets.Graphics.Quads;

public sealed class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private partial struct Push
    {
        public Matrix4 Projection;
        public ulong Buffer;
    }
    
    private readonly IGraphicsShader _batchRslShader = RslGraphicsShader.FromFile(Path.Join(SRuntime.ResourcesDirectory, "shaders", "widgets", "batch.rsl"));
    
    protected override IGraphicsShader GetShader() => _batchRslShader;

    protected override QuadBatch MakeNewBatch() => new QuadBatch();

    protected override uint WriteBatch(WidgetFrame frame, IDeviceBuffer view, QuadBatch batch, IGraphicsShader shader)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        var resourceSet = SGraphicsModule.Get().GetResourceManager().GetDescriptorSet();
        var quads = batch.GetQuads().ToArray();
        view.Write(quads);
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
            _batchRslShader.GetPipelineLayout(), new DescriptorSet[] {resourceSet});
            
        var pushResource = _batchRslShader.PushConstants.First().Value;
        var push = new Push()
        {
            Projection = frame.Projection,
            Buffer = view.GetAddress()
                    
        };
        cmd.PushConstant(_batchRslShader.GetPipelineLayout(), pushResource.Stages,push);
        return (uint)quads.Length;
    }
}