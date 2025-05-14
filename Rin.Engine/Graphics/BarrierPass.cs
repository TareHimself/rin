using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

internal class PassResourceSync
{
    public required uint ResourceId { get; set; }
    public required uint PassId { get; set; }
}

internal class ImageResourceSync : PassResourceSync
{
    public required ImageLayout PreviousLayout { get; set; }
    public required ImageLayout NextLayout { get; set; }
    public required ResourceUsage PreviousUsage { get; set; }
    public required ResourceUsage NextUsage { get; set; }
}

internal class BufferResourceSync : PassResourceSync
{
    public required BufferStage PreviousStage { get; set; }
    public required BufferStage NextStage { get; set; }
    public required ResourceUsage PreviousUsage { get; set; }
    public required ResourceUsage NextUsage { get; set; }
}

internal class BarrierPass(IEnumerable<BufferResourceSync> buffers, IEnumerable<ImageResourceSync> images) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphConfig config)
    {
        throw new Exception("HOW HAVE YOU DONE THIS?");
    }

    public void Execute(ICompiledGraph graph, in IExecutionContext ctx)
    {
        using var commandContext = ctx.UsingCmd();
        var cmd = commandContext.Get();
        
        
        foreach (var bufferResourceSync in buffers)
        {
            var buffer = graph.GetBufferOrException(bufferResourceSync.ResourceId);
            // Console.WriteLine("GRAPH :: Buffer Barrier :: Resource Id {0},Transition {1} => {2}",bufferResourceSync.ResourceId,bufferResourceSync.PreviousUsage,bufferResourceSync.NextUsage);
            cmd.BufferBarrier(buffer, bufferResourceSync.PreviousStage, bufferResourceSync.NextStage);
        }

        foreach (var imageResourceSync in images)
        {
            var image = graph.GetImageOrException(imageResourceSync.ResourceId);
            // Console.WriteLine("GRAPH :: Image Barrier :: Resource Id {0}, Format {1},Transition {2} => {3}",imageResourceSync.ResourceId,image.Format,imageResourceSync.PreviousLayout,imageResourceSync.NextLayout);
            cmd.ImageBarrier(image, imageResourceSync.PreviousLayout, imageResourceSync.NextLayout);
        }
    }
}