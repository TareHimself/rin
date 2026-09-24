using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

internal sealed class TextureUploadPass(ResourceHandle texture, uint resourceId, bool discard) : IUploadPass
{
    private DeviceBufferView _staging;
    private PendingTextureWrite? _write;

    public ResourceHandle Texture => texture;

    public void Dispose()
    {
        _write?.Dispose();
        _write = null;
    }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        ((GraphConfig)config).PrependTextureWrite(resourceId, discard);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_write is null) return;

        var module = VulkanGraphicsModule.Get();
        var image = module.GetTexture(graph.GetImageOrException(resourceId));
        Debug.Assert(image is not null, "Upload target is not resolvable");
        WriteRecorder.RecordTextureCopy(((VulkanExecutionContext)ctx).CommandBuffer, image!, _staging, _write);
        module.BindPendingTexture(texture);
    }

    public void Assign(PendingTextureWrite write, in DeviceBufferView staging)
    {
        _write = write;
        _staging = staging;
    }

    public void OnSubmitted()
    {
        _write?.Complete();
        Dispose();
    }
}
