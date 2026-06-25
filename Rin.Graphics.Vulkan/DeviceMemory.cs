namespace Rin.Graphics.Vulkan;

/// <summary>
///     GPU Memory
/// </summary>
public interface IVulkanDeviceMemory : IDisposable
{
    public IntPtr Allocator { get; }
    public IntPtr Allocation { get; }
}