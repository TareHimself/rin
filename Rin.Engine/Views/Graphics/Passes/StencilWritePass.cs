using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.Views.Graphics.Passes;

public class StencilWritePass : IPass
{
    private readonly SharedPassContext _sharedContext;
    private StencilClip[] _clips;
    private uint _mask;
    public StencilWritePass(SharedPassContext sharedContext,uint mask,StencilClip[] clips)
    {
        _sharedContext = sharedContext;
        _mask = mask;
        _clips = clips;
    }

    private uint MainImageId => _sharedContext.MainImageId;
    private uint CopyImageId => _sharedContext.CopyImageId;
    private uint StencilImageId => _sharedContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public void Added(IGraphBuilder builder)
    {
        
    }

    public void Configure(IGraphConfig config)
    {
        config.Write(StencilImageId);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var image = graph.GetImageOrException(StencilImageId);
        
        var clearAttachment = new VkClearAttachment
        {
            aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
            clearValue = new VkClearValue
            {
                color = SGraphicsModule.MakeClearColorValue(Vector4.Zero),
                depthStencil = new VkClearDepthStencilValue
                {
                    stencil = 0
                }
            }
        };
        unsafe
        {
            var clearRect = new VkClearRect
            {
                baseArrayLayer = 0,
                layerCount = 1,
                rect = new VkRect2D
                {
                    offset = new VkOffset2D
                    {
                        x = 0,
                        y = 0
                    },
                    extent = new VkExtent2D
                    {
                        width = image.Extent.Width,
                        height = image.Extent.Height
                    }
                }
            };
            
            var cmd = frame.GetCommandBuffer();
            vkCmdClearAttachments(cmd, 1, &clearAttachment, 1, &clearRect);
        }
    }
}