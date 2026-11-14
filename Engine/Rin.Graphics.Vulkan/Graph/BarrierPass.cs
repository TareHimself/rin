using Rin.Core.Shared.Pooling;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

internal readonly record struct ImageResourceSync(
    uint ResourceId,
    ImageLayout PreviousLayout,
    ImageLayout NextLayout,
    ResourceOperation NextOperation,
    bool FromCurrentLayout = false);

internal readonly record struct BufferResourceSync(
    uint ResourceId,
    GraphBufferUsage PreviousUsage,
    GraphBufferUsage NextUsage,
    ResourceOperation PreviousOperation,
    ResourceOperation NextOperation);

/// <summary>
///     Applies the barriers one level of passes needs. A compiled graph reuses its barrier passes across frames.
/// </summary>
internal class BarrierPass : IPass, IResettable
{
    private const int MaxStackBarriers = 6;

    public List<BufferResourceSync> Buffers { get; } = [];
    public List<ImageResourceSync> Images { get; } = [];

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        throw new Exception("HOW HAVE YOU DONE THIS?");
    }

    public void Reset()
    {
        Buffers.Clear();
        Images.Clear();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (ctx is VulkanExecutionContext vkCtx)
        {
            using var bufferScratch = new ScratchSpan<BufferBarrier>(Buffers.Count,
                Buffers.Count <= MaxStackBarriers ? stackalloc BufferBarrier[Buffers.Count] : default);
            using var imageScratch = new ScratchSpan<TextureBarrier>(Images.Count,
                Images.Count <= MaxStackBarriers ? stackalloc TextureBarrier[Images.Count] : default);
            var bufferBarriers = bufferScratch.Span;
            var imageBarriers = imageScratch.Span;

            for (var i = 0; i < Buffers.Count; i++)
            {
                var sync = Buffers[i];
                bufferBarriers[i].View = graph.GetBufferOrException(sync.ResourceId);
                bufferBarriers[i].From = sync.PreviousUsage;
                bufferBarriers[i].To = sync.NextUsage;
                bufferBarriers[i].FromOperation = sync.PreviousOperation;
                bufferBarriers[i].ToOperation = sync.NextOperation;
            }
            var imageCount = 0;
            foreach (var sync in Images)
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