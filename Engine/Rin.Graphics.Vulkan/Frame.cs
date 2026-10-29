using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Graphics.Vulkan.Graph;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     One slot of the module's frame ring: the command buffer, fence, descriptor allocator and graph builder one
///     rendered frame uses. Disposables hooked on <see cref="OnReset" /> are released when the slot is reused.
/// </summary>
public class Frame : IDisposable
{
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkCommandPool _commandPool;
    private readonly DescriptorAllocator _descriptorAllocator;
    private readonly VkDevice _device;
    private readonly VkFence _renderFence;
    private bool _rendering;

    public Frame(VulkanGraphicsModule module, uint index)
    {
        _descriptorAllocator = new DescriptorAllocator(1000, [
            new PoolSizeRatio(DescriptorType.StorageImage, 3),
            new PoolSizeRatio(DescriptorType.StorageBuffer, 3),
            new PoolSizeRatio(DescriptorType.UniformBuffer, 3),
            new PoolSizeRatio(DescriptorType.CombinedSamplerImage, 4)
        ]);

        Index = index;
        _device = module.GetDevice();
        _commandPool = _device.CreateCommandPool(module.GetGraphicsQueueFamily());
        _commandBuffer = _device.AllocateCommandBuffers(_commandPool).First();
        _renderFence = _device.CreateFence(true);
        ExecutionContext = new VulkanExecutionContext(_commandBuffer, _descriptorAllocator, module.GlobalDescriptors);
        GraphBuilder = new GraphBuilder(module.ResourcePool, this);
    }

    /// <summary>
    ///     Position in the module's frame ring, which window renderers use to pick per-frame resources of their own.
    /// </summary>
    public uint Index { get; }

    public VulkanExecutionContext ExecutionContext { get; }
    public GraphBuilder GraphBuilder { get; }

    public void Dispose()
    {
        WaitForLastDraw();
        OnReset?.Invoke(this);
        OnReset = null;
        _descriptorAllocator.Dispose();
        _device.DestroyFence(_renderFence);
        _device.DestroyCommandPool(_commandPool);
    }

    public event Action<Frame>? OnReset;

    public VkFence GetRenderFence()
    {
        return _renderFence;
    }

    public VkCommandBuffer GetPrimaryCommandBuffer()
    {
        return _commandBuffer;
    }

    public DescriptorAllocator GetDescriptorAllocator()
    {
        return _descriptorAllocator;
    }

    /// <summary>
    ///     Waits for the GPU to finish the last frame recorded in this slot.
    /// </summary>
    public void WaitForLastDraw()
    {
        if (!_rendering) return;
        _device.WaitForFences(ulong.MaxValue, true, _renderFence);
        _rendering = false;
    }

    public void Finish()
    {
        _rendering = true;
    }

    /// <summary>
    ///     Releases what the last frame in this slot left behind and readies the slot for the next one.
    /// </summary>
    public void Reset()
    {
        OnReset?.Invoke(this);
        OnReset = null;
        _descriptorAllocator.ClearPools();
        _device.ResetFences(_renderFence);
    }
}
