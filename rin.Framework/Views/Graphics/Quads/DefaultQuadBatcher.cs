﻿using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Views.Graphics.Quads;

[ViewsBatcher]
public partial class DefaultQuadBatcher : SimpleQuadBatcher<QuadBatch>
{
    
    private partial struct Push
    {
        public Mat4 Projection;
        public ulong Buffer;
    }

    private readonly IGraphicsShader _batchShader = SGraphicsModule.Get().GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory ,"views", "batch.slang"));
    
    protected override IGraphicsShader GetShader() => _batchShader;

    protected override QuadBatch MakeNewBatch() => new QuadBatch();

    protected override uint WriteBatch(ViewsFrame frame, IDeviceBufferView view, QuadBatch batch,
        IGraphicsShader shader)
    {
        var cmd = frame.Raw.GetCommandBuffer();
        var resourceSet = SGraphicsModule.Get().GetTextureManager().GetDescriptorSet();
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