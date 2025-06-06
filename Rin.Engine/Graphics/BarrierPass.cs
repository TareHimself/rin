using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Graphics;

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

internal class BarrierPass(IEnumerable<BufferResourceSync> buffers, IEnumerable<ImageResourceSync> images) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        throw new Exception("HOW HAVE YOU DONE THIS?");
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        foreach (var bufferResourceSync in buffers)
        {
            var buffer = graph.GetBufferOrException(bufferResourceSync.ResourceId);
            ctx.Barrier(buffer, bufferResourceSync.PreviousUsage, bufferResourceSync.NextUsage,
                bufferResourceSync.PreviousOperation, bufferResourceSync.NextOperation);
        }

        foreach (var imageResourceSync in images)
        {
            var image = graph.GetImageOrException(imageResourceSync.ResourceId);
            ctx.Barrier(image, imageResourceSync.PreviousLayout, imageResourceSync.NextLayout);
        }
    }
}