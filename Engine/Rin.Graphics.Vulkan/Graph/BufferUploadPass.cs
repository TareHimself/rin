using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

internal sealed class BufferUploadPass(ResourceHandle buffer, IReadOnlyList<uint> resourceIds) : IUploadPass
{
    private DeviceBufferView _staging;
    private readonly List<PendingBufferWrite> _writes = [];

    public ResourceHandle Buffer => buffer;

    public void Dispose()
    {
        foreach (var write in _writes) write.Dispose();
        _writes.Clear();
    }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        foreach (var resourceId in resourceIds) ((GraphConfig)config).PrependBufferWrite(resourceId);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_writes.Count == 0) return;
        WriteRecorder.RecordBufferWrites(((VulkanExecutionContext)ctx).CommandBuffer, buffer, _staging, _writes);
    }

    public void Assign(List<PendingBufferWrite> writes, in DeviceBufferView staging)
    {
        _writes.AddRange(writes);
        _staging = staging;
    }

    public void OnSubmitted()
    {
        foreach (var write in _writes) write.Complete();
        Dispose();
    }
}
