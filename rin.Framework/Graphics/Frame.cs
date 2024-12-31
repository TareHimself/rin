using rin.Framework.Core;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

using static VkStructureType;
using static Vulkan;

/// <summary>
///     Represents a <see cref="WindowRenderer" /> frame
/// </summary>
public class Frame : Disposable
{
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkCommandPool _commandPool;
    private readonly DescriptorAllocator _descriptorAllocator;
    private readonly VkDevice _device;
    private readonly VkFence _renderFence;
    private readonly VkSemaphore _renderSemaphore;
    private readonly VkSemaphore _swapchainSemaphore;
    public readonly WindowRenderer Renderer;
    private readonly IGraphBuilder _graphBuilder = new GraphBuilder();
    
    public event Action<Frame, VkImage, VkExtent2D>? OnCopy;

    public unsafe Frame(WindowRenderer renderer)
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

    public void DoCopy(VkImage swapchainImage,VkExtent2D extent)
    {
        OnCopy?.Invoke(this,swapchainImage,extent);
    }
    public event Action<Frame>? OnReset;


    public IGraphBuilder GetBuilder() => _graphBuilder;
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

    public VkCommandBuffer GetCommandBuffer()
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
    public void WaitForLastDraw()
    {
        var r = _device.WaitForFences(ulong.MaxValue,true,_renderFence);
        if (r != VkResult.VK_SUCCESS)
        {
            throw new Exception("Failed to wait for fences");
        }
    }

    /// <summary>
    ///     Resets this <see cref="Frame" />
    /// </summary>
    public void Reset()
    {
        OnReset?.Invoke(this);
        OnReset = null;
        OnCopy = null;
        _descriptorAllocator.ClearPools();
        var r = _device.ResetFences(_renderFence);
        if (r != VkResult.VK_SUCCESS)
        {
            throw new Exception("Failed to reset fences");
        }
        _graphBuilder.Reset();
    }

    protected override void OnDispose(bool isManual)
    {
        SGraphicsModule.Get().WaitDeviceIdle();
        OnReset?.Invoke(this);
        OnReset = null;
        OnCopy = null;
        _descriptorAllocator.Dispose();
        _device.DestroySemaphore(_swapchainSemaphore);
        _device.DestroySemaphore(_renderSemaphore);
        _device.DestroyFence(_renderFence);
        _device.DestroyCommandPool(_commandPool);
    }
}