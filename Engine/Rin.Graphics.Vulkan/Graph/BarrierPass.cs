using Rin.Core.Shared.Pooling;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

internal class PassResourceSync
{
    public required uint ResourceId { get; set; }
    public required uint PassId { get; set; }

    public required ResourceOperation PreviousOperation { get; set; }

    public required ResourceOperation NextOperation { get; set; }
}

internal class ImageResourceSync : PassResourceSync
{
    public required ImageLayout PreviousLayout { get; set; }
    public required ImageLayout NextLayout { get; set; }
    public bool FromCurrentLayout { get; init; }
}

internal class BufferResourceSync : PassResourceSync
{
    public required GraphBufferUsage PreviousUsage { get; set; }
    public required GraphBufferUsage NextUsage { get; set; }
}

internal class BarrierPass(BufferResourceSync[] buffers, ImageResourceSync[] images) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        throw new Exception("HOW HAVE YOU DONE THIS?");
    }

    private const int MaxStackBarriers = 6;

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (ctx is VulkanExecutionContext vkCtx)
        {
            using var bufferScratch = new ScratchSpan<BufferBarrier>(buffers.Length,
                buffers.Length <= MaxStackBarriers ? stackalloc BufferBarrier[buffers.Length] : default);
            using var imageScratch = new ScratchSpan<TextureBarrier>(images.Length,
                images.Length <= MaxStackBarriers ? stackalloc TextureBarrier[images.Length] : default);
            var bufferBarriers = bufferScratch.Span;
            var imageBarriers = imageScratch.Span;

            for (var i = 0; i < buffers.Length; i++)
            {
                bufferBarriers[i].View = graph.GetBufferOrException(buffers[i].ResourceId);
                bufferBarriers[i].From = buffers[i].PreviousUsage;
                bufferBarriers[i].To = buffers[i].NextUsage;
                bufferBarriers[i].FromOperation = buffers[i].PreviousOperation;
                bufferBarriers[i].ToOperation = buffers[i].NextOperation;
            }
            var imageCount = 0;
            foreach (var sync in images)
            {
                var texture = graph.GetImageOrException(sync.ResourceId);
                var from = sync.PreviousLayout;
                if (sync.FromCurrentLayout)
                {
                    from = VulkanGraphicsModule.Get().GetImage(texture)!.Layout;
                    if (from == sync.NextLayout && sync.NextOperation == ResourceOperation.Read) continue;
                }

                imageBarriers[imageCount].Texture = texture;
                imageBarriers[imageCount].From = from;
                imageBarriers[imageCount].To = sync.NextLayout;
                imageCount++;
            }

            vkCtx.Barrier(bufferBarriers);
            vkCtx.Barrier(imageBarriers[..imageCount]);
        }
    }
}