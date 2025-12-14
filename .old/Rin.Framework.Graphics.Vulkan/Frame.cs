using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan;
using Rin.Framework.Graphics.Vulkan.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

/// <summary>
///     Represents a <see cref="WindowRenderer" /> frame
/// </summary>
public class Frame : IDisposable
{
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkCommandPool _commandPool;
    private readonly DescriptorAllocator _descriptorAllocator;
    private readonly VkDevice _device;
    private readonly VkFence _renderFence;
    private readonly VkSemaphore _swapchainSemaphore;
    public readonly WindowRenderer Renderer;
    private bool _rendering;

    public Frame(WindowRenderer renderer)
    {
        _descriptorAllocator = new DescriptorAllocator(1000, [
            new PoolSizeRatio(DescriptorType.StorageImage, 3),
            new PoolSizeRatio(DescriptorType.StorageBuffer, 3),
            new PoolSizeRatio(DescriptorType.UniformBuffer, 3),
            new PoolSizeRatio(DescriptorType.CombinedSamplerImage, 4)
        ]);

        Renderer = renderer;
        var subsystem = VulkanGraphicsModule.Get();
        var device = subsystem.GetDevice();
        var queueFamily = subsystem.GetGraphicsQueueFamily();

        _device = device;

        _commandPool = device.CreateCommandPool(queueFamily);
        _commandBuffer = device.AllocateCommandBuffers(_commandPool).First();
        _renderFence = device.CreateFence(true);
        _swapchainSemaphore = device.CreateSemaphore();
    }


    public void Dispose()
    {
        WaitForLastDraw();
        OnReset?.Invoke(this);
        OnReset = null;
        _descriptorAllocator.Dispose();
        _device.DestroySemaphore(_swapchainSemaphore);
        _device.DestroyFence(_renderFence);
        _device.DestroyCommandPool(_commandPool);
    }

    public event Action<Frame>? OnReset;


    public VkFence GetRenderFence()
    {
        return _renderFence;
    }

    public VkSemaphore GetSwapchainSemaphore()
    {
        return _swapchainSemaphore;
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
    ///     Wait's for the previous frame to complete
    /// </summary>
    public VkResult WaitForLastDraw()
    {
        if (_rendering)
        {
            var r = _device.WaitForFences(ulong.MaxValue, true, _renderFence);
            _rendering = false;
            return r;
        }

        return VkResult.VK_SUCCESS;
    }

    public void Finish()
    {
        _rendering = true;
    }

    /// <summary>
    ///     Resets this <see cref="Frame" />
    /// </summary>
    public void Reset()
    {
        OnReset?.Invoke(this);
        OnReset = null;
        _descriptorAllocator.ClearPools();
        var r = _device.ResetFences(_renderFence);
        if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");
        //_graphBuilder.Reset();
    }
}