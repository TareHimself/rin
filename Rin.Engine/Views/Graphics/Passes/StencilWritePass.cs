using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Views.Graphics.Passes;

public class StencilWritePass : IPass
{
    private readonly StencilClip[] _clips;
    private readonly SharedPassContext _sharedContext;
    private uint _mask;

    private readonly IShader _stencilShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/stencil_batch.slang");

    public StencilWritePass(SharedPassContext sharedContext, uint mask, StencilClip[] clips)
    {
        _sharedContext = sharedContext;
        _mask = mask;
        _clips = clips;
    }
    
    private uint StencilImageId => _sharedContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    private uint _clipsBufferId;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(StencilImageId, ImageLayout.StencilAttachment);
        _clipsBufferId = config.CreateBuffer<StencilClip>(_clips.Length, BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
       
        if (_stencilShader.Bind(cmd, true))
        {
            var stencilImage = graph.GetImageOrException(StencilImageId);
            var clipsBuffer = graph.GetBufferOrException(_clipsBufferId);
            clipsBuffer.Write(_clips);
            cmd.BeginRendering(_sharedContext.Extent.ToVk(), [],
                stencilAttachment: stencilImage.MakeStencilAttachmentInfo())
                .SetViewState(_sharedContext.Extent);
            
            var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
            
            vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkCompareOp.VK_COMPARE_OP_ALWAYS);
            cmd.SetWriteMask(0, 1, 0);
            
            _stencilShader.Push(cmd,clipsBuffer.GetAddress());
            cmd.Draw(6,(uint)_clips.Length);
            cmd.EndRendering();
        }
    }
}