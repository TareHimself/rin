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
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
        var device = subsystem.GetDevice();
        var queueFamily = subsystem.GetGraphicsQueueFamily();

        _device = device;

        var cmdPoolCreateInfo = new VkCommandPoolCreateInfo
        {
            sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = queueFamily
        };


        VkCommandPool cmdPool;

        vkCreateCommandPool(device, &cmdPoolCreateInfo, null, &cmdPool);

        _commandPool = cmdPool;

        var cmdBufferAllocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = cmdPool,
            commandBufferCount = 1,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY
        };

        VkCommandBuffer buff;

        vkAllocateCommandBuffers(device, &cmdBufferAllocInfo, &buff);

        _commandBuffer = buff;

        var fenceCreateInfo = new VkFenceCreateInfo
        {
            sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT
        };

        var semaphoreCreateInfo = new VkSemaphoreCreateInfo
        {
            sType = VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO
        };

        VkFence rFence;
        vkCreateFence(device, &fenceCreateInfo, null, &rFence);

        _renderFence = rFence;

        VkSemaphore rSemaphore, sSemaphore;
        vkCreateSemaphore(device, &semaphoreCreateInfo, null, &rSemaphore);
        vkCreateSemaphore(device, &semaphoreCreateInfo, null, &sSemaphore);

        _renderSemaphore = rSemaphore;
        _swapchainSemaphore = sSemaphore;
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
        unsafe
        {
            // var status = vkGetFenceStatus(_device, _renderFence);
            // if (status == VkResult.VK_NOT_READY)
            // {
            //     fixed (VkFence* pFence = &_renderFence)
            //     {
            //         var r = vkWaitForFences(_device, 1, pFence, 1, ulong.MaxValue);
            //         if (r != VkResult.VK_SUCCESS)
            //         {
            //         
            //         }
            //     }
            // }
            fixed (VkFence* pFence = &_renderFence)
            {
                var r = vkWaitForFences(_device, 1, pFence, 1, ulong.MaxValue);
                if (r != VkResult.VK_SUCCESS)
                {
                    
                }
            }
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
        unsafe
        {
            fixed (VkFence* pFence = &_renderFence)
            {
                var r = vkResetFences(_device, 1, pFence);
                if (r != VkResult.VK_SUCCESS)
                {
                    throw new Exception("Failed to reset fences");
                }
            }
        }
        _graphBuilder.Reset();
    }

    protected override unsafe void OnDispose(bool isManual)
    {
        var device = SGraphicsModule.Get().GetDevice();
        SGraphicsModule.Get().WaitDeviceIdle();
        OnReset?.Invoke(this);
        OnReset = null;
        OnCopy = null;
        _descriptorAllocator.Dispose();
        vkDestroySemaphore(device, _swapchainSemaphore, null);
        vkDestroySemaphore(device, _renderSemaphore, null);
        vkDestroyFence(device, _renderFence, null);
        vkDestroyCommandPool(device, _commandPool, null);
    }
}