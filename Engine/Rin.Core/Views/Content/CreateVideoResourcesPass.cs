using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Content;

/// <summary>
///     Uploads each command's NV12 frame into a luma (R8) and a chroma (RG8) texture from one staging buffer.
/// </summary>
internal class CreateVideoResourcesPass(VideoCommand[] commands) : IPass, IDisposable
{
    public uint[] LumaImageIds = [];
    public uint[] ChromaImageIds = [];
    private uint[] _videoStagingBufferIds = [];
    public uint Id { get; set; }

    // Whether this pass ran or got pruned, the decoded frame data behind it is owned here and
    // released exactly once - the graph calls Dispose in either case.
    public void Dispose()
    {
        foreach (var command in commands) command.FrameData.Dispose();
    }

    public void Configure(IGraphConfig config)
    {
        _videoStagingBufferIds = commands
            .Select(c => config.CreateBuffer(c.FrameData.GetByteSize(), GraphBufferUsage.HostThenTransfer)).ToArray();
        LumaImageIds = commands
            .Select(c => config.CreateTexture(c.Planes.Extent, ImageFormat.R8, ImageLayout.TransferDst)).ToArray();
        ChromaImageIds = commands
            .Select(c => config.CreateTexture(c.Planes.ChromaExtent, ImageFormat.RG8, ImageLayout.TransferDst)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        for (var i = 0; i < commands.Length; i++)
        {
            var command = commands[i];
            var planes = command.Planes;
            var stagingBuffer = graph.GetBufferOrException(_videoStagingBufferIds[i]);
            stagingBuffer.Write(command.FrameData);
            ctx.CopyToImage(stagingBuffer.GetView(0, planes.LumaByteSize), graph.GetImage(LumaImageIds[i]));
            ctx.CopyToImage(stagingBuffer.GetView(planes.LumaByteSize, planes.ChromaByteSize),
                graph.GetImage(ChromaImageIds[i]));
        }
    }
}
