using Rin.Engine.Extensions;
using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

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
    private readonly VkSemaphore _renderSemaphore;
    private readonly VkSemaphore _swapchainSemaphore;
    public readonly WindowRenderer Renderer;
    private LinkedList<VkCommandBuffer> _secondaryCommandBuffers = [];
    private bool _rendering;

    public Frame(WindowRenderer renderer)
    {
        _descriptorAllocator = new DescriptorAllocator(1000, [
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 4)
        ]);

        Renderer = renderer;
        var subsystem = SGraphicsModule.Get();
        var device = subsystem.GetDevice();
        var queueFamily = subsystem.GetGraphicsQueueFamily();

        _device = device;

        _commandPool = device.CreateCommandPool(queueFamily);
        _commandBuffer = device.AllocateCommandBuffers(_commandPool).First();
        _renderFence = device.CreateFence(true);
        _renderSemaphore = device.CreateSemaphore();
        _swapchainSemaphore = device.CreateSemaphore();
    }
    
    public VkCommandBuffer AllocateSecondaryCommandBuffer()
    {
        if (_secondaryCommandBuffers.Count > 0)
        {
            var cmd = _secondaryCommandBuffers.First();
            _secondaryCommandBuffers.RemoveFirst();
            return cmd;
        }

        return _device.AllocateCommandBuffers(_commandPool,
            uint.Min((uint)Environment.ProcessorCount, 1),
            VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_SECONDARY)[0];
    }

    public IEnumerable<VkCommandBuffer> AllocateSecondaryCommandBuffers(uint count)
    {
        
        var total = 0;

        while (_secondaryCommandBuffers.Count > 0 && total != count)
        {
            yield return _secondaryCommandBuffers.First();
            _secondaryCommandBuffers.RemoveFirst();
            total++;
        }

        if (total != count)
        {
            foreach (var cmd in _device.AllocateCommandBuffers(_commandPool,
                         uint.Min((uint)Environment.ProcessorCount, 6),
                         VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_SECONDARY))
            {
                yield return cmd;
            }
        }
    }
    
    public void FreeCommandBuffers(IEnumerable<VkCommandBuffer> commandBuffers)
    {
        foreach (var cmd in commandBuffers)
        {
            _secondaryCommandBuffers.AddLast(cmd);
        }
    }

    public void Dispose()
    {
        WaitForLastDraw();
        OnReset?.Invoke(this);
        OnReset = null;
        _descriptorAllocator.Dispose();
        _device.DestroySemaphore(_swapchainSemaphore);
        _device.DestroySemaphore(_renderSemaphore);
        _device.DestroyFence(_renderFence);
        _device.DestroyCommandPool(_commandPool);
    }

    public event Action<Frame>? OnReset;


    public VkFence GetRenderFence()
    {
        return _renderFence;
    }

    public VkSemaphore GetRenderSemaphore()
    {
        return _renderSemaphore;
    }

    public VkSemaphore GetSwapchainSemaphore()
    {
        return _swapchainSemaphore;
    }

    public VkCommandBuffer GetPrimaryCommandBuffer()
    {
        return _commandBuffer;
    }
    
    public IEnumerable<VkCommandBuffer> GetCommandBuffers()
    {
        yield return _commandBuffer;
        foreach (var secondaryCommandBuffer in _secondaryCommandBuffers) yield return secondaryCommandBuffer;
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