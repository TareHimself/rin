using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Runs a queued copy: reads its source and writes its destination, both ahead of the graph's own users.
/// </summary>
internal sealed class CopyUploadPass(
    SharedWrite shared,
    IReadOnlyList<uint> sourceIds,
    IReadOnlyList<uint> destinationIds) : IUploadPass
{
    private bool _taken;

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        var graphConfig = (GraphConfig)config;
        if (!shared.IsSingleLane)
            foreach (var id in sourceIds)
                if (shared.Source.Handle.Type == ResourceType.Texture)
                    graphConfig.PrependTextureRead(id);
                else
                    graphConfig.PrependBufferRead(id);

        foreach (var id in destinationIds)
            if (shared.Destination.Handle.Type == ResourceType.Texture)
                graphConfig.PrependTextureWrite(id, false);
            else
                graphConfig.PrependBufferWrite(id);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (!_taken) return;

        var context = (VulkanExecutionContext)ctx;
        switch (shared.Action)
        {
            case BufferCopy buffers:
                context.CopyToBuffer(new DeviceBufferView(buffers.Source, buffers.SourceOffset, buffers.Size),
                    new DeviceBufferView(buffers.Destination, buffers.DestinationOffset, buffers.Size));
                break;
            case TextureCopy textures:
                context.CopyImage(textures.Source, textures.SourceOffset, textures.Destination,
                    textures.DestinationOffset, textures.Extent);
                break;
            case BufferToTextureCopy toTexture:
            {
                var image = VulkanGraphicsModule.Get().GetTexture(toTexture.Destination);
                Debug.Assert(image is not null, "Copy destination is not resolvable");
                WriteRecorder.RecordBufferToImage(context.CommandBuffer, image!,
                    new DeviceBufferView(toTexture.Source, toTexture.SourceOffset, toTexture.Size),
                    toTexture.DestinationOffset, toTexture.Extent);
                break;
            }
            case TextureToBufferCopy toBuffer:
            {
                var module = VulkanGraphicsModule.Get();
                var image = module.GetTexture(toBuffer.Source);
                var buffer = module.ResolveBuffer(toBuffer.Destination);
                Debug.Assert(image is not null && buffer is not null, "Copy resource is not resolvable");
                context.CommandBuffer.CopyImageToBuffer(image!, toBuffer.SourceOffset, toBuffer.Extent, buffer!,
                    toBuffer.DestinationOffset);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(shared), shared.Action, null);
        }
    }

    public void Assign()
    {
        _taken = true;
    }

    public void OnSubmitted()
    {
        if (!_taken) return;
        shared.Complete();
        Dispose();
    }

    public void Dispose()
    {
        if (_taken) shared.Dispose();
    }
}
