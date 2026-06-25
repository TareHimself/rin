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

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var bufferBarriers = new BufferBarrier[buffers.Length];
        var imageBarriers = new TextureBarrier[images.Length];
        
        for (var i = 0; i < buffers.Length; i++)
        {
            bufferBarriers[i].View = graph.GetBufferOrException(images[i].ResourceId);
            bufferBarriers[i].From = buffers[i].PreviousUsage;
            bufferBarriers[i].To = buffers[i].NextUsage;
            bufferBarriers[i].FromOperation = buffers[i].PreviousOperation;
            bufferBarriers[i].ToOperation = buffers[i].NextOperation;
        }
        for (var i = 0; i < images.Length; i++)
        {
            imageBarriers[i].Texture = graph.GetTextureOrException(images[i].ResourceId);
            imageBarriers[i].From = images[i].PreviousLayout;
            imageBarriers[i].To = images[i].NextLayout;
        }

        ctx.Barrier(bufferBarriers);
        ctx.Barrier(imageBarriers);
        foreach (var bufferResourceSync in buffers)
        {
            var buffer = graph.GetBufferOrException(bufferResourceSync.ResourceId);
            ctx.Barrier(buffer, bufferResourceSync.PreviousUsage, bufferResourceSync.NextUsage,
                bufferResourceSync.PreviousOperation, bufferResourceSync.NextOperation);
        }

        foreach (var imageResourceSync in images)
        {
            var image = graph.GetTexture(imageResourceSync.ResourceId);
            ctx.Barrier(image, imageResourceSync.PreviousLayout, imageResourceSync.NextLayout);
        }
    }
}