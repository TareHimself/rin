using System.Buffers;
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
}

internal class BufferResourceSync : PassResourceSync
{
    public required BufferUsage PreviousUsage { get; set; }
    public required BufferUsage NextUsage { get; set; }
}

internal class BarrierPass(BufferResourceSync[] buffers, ImageResourceSync[] images) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        throw new Exception("HOW HAVE YOU DONE THIS?");
    }

    private const int MaxStackBarriers = 6;

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var rentedBuffers = buffers.Length > MaxStackBarriers
            ? ArrayPool<BufferBarrier>.Shared.Rent(buffers.Length)
            : null;
        var rentedImages = images.Length > MaxStackBarriers
            ? ArrayPool<TextureBarrier>.Shared.Rent(images.Length)
            : null;
        try
        {
            var bufferBarriers = rentedBuffers is not null
                ? rentedBuffers.AsSpan(0, buffers.Length)
                : stackalloc BufferBarrier[buffers.Length];
            var imageBarriers = rentedImages is not null
                ? rentedImages.AsSpan(0, images.Length)
                : stackalloc TextureBarrier[images.Length];

            for (var i = 0; i < buffers.Length; i++)
            {
                bufferBarriers[i].View = graph.GetBufferOrException(buffers[i].ResourceId);
                bufferBarriers[i].From = buffers[i].PreviousUsage;
                bufferBarriers[i].To = buffers[i].NextUsage;
                bufferBarriers[i].FromOperation = buffers[i].PreviousOperation;
                bufferBarriers[i].ToOperation = buffers[i].NextOperation;
            }
            for (var i = 0; i < images.Length; i++)
            {
                imageBarriers[i].Texture = graph.GetImageOrException(images[i].ResourceId);
                imageBarriers[i].From = images[i].PreviousLayout;
                imageBarriers[i].To = images[i].NextLayout;
            }

            ctx.Barrier(bufferBarriers);
            ctx.Barrier(imageBarriers);
        }
        finally
        {
            if (rentedBuffers is not null) ArrayPool<BufferBarrier>.Shared.Return(rentedBuffers);
            if (rentedImages is not null) ArrayPool<TextureBarrier>.Shared.Return(rentedImages);
        }
    }
}