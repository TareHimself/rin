using JetBrains.Annotations;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
/// A <see cref="IDeviceBufferView"/> for skinned vertices, <see cref="DeviceBuffer"/> and <see cref="GetView"/> will throw <see cref="NullReferenceException"/> unless <see cref="UnderlyingView"/> has been set
/// </summary>
public class SkinnedVertexBufferView(ulong defaultOffset, ulong defaultSize) : IDeviceBufferView
{
    [PublicAPI] public IDeviceBufferView? UnderlyingView { get; set; }

    public IDeviceBuffer DeviceBuffer => UnderlyingView?.DeviceBuffer ?? throw new NullReferenceException();
    public ulong Offset => UnderlyingView?.Offset ?? defaultOffset;

    public ulong Size => UnderlyingView?.Size ?? defaultSize;

    public VkBuffer NativeBuffer => UnderlyingView?.NativeBuffer ?? throw new NullReferenceException();
    public IDeviceBufferView GetView(ulong offset, ulong size)
    {
        return UnderlyingView?.GetView(offset,size) ?? throw new NullReferenceException();
    }
}