using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Meshes;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Descriptors;
using Rin.Framework.Graphics.Vulkan.Images;
using Rin.Framework.Graphics.Vulkan.Meshes;
using Rin.Framework.Graphics.Vulkan.Shaders.Slang;
using Rin.Framework.Graphics.Windows;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Vulkan;

public partial class VulkanGraphicsModule : IGraphicsModule
{
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly DescriptorLayoutFactory _descriptorLayoutFactory = new();
    private readonly int _maxEventsPerPeep = 64;
    private readonly List<Pair<TaskCompletionSource, Action<IExecutionContext>>> _pendingGraphicsSubmits = [];
    private readonly List<Pair<TaskCompletionSource, Action<IExecutionContext>>> _pendingTransferSubmits = [];
    private readonly List<IRenderer> _renderers = [];
    private readonly Dictionary<ulong, RinWindow> _rinWindows = [];
    private readonly BackgroundTaskQueue _transferQueueThread = new();
    private readonly Dictionary<IWindow, IWindowRenderer> _windows = [];
    private IRenderData[] _collected = [];
    private VkDebugUtilsMessengerEXT _debugUtilsMessenger;
    private DescriptorAllocator? _descriptorAllocator;
    private VkDevice _device;
    private VkCommandBuffer _graphicsCommandBuffer;
    private VkCommandPool _graphicsCommandPool;
    private VkFence _graphicsFence;
    private VkQueue _graphicsQueue;
    private uint _graphicsQueueFamily;
    private bool _hasDedicatedTransferQueue;
    private VkInstance _instance;
    private IMeshFactory? _meshFactory;
    private VkPhysicalDevice _physicalDevice;
    private SamplerFactory? _samplerFactory;
    private IShaderManager? _shaderManager;

    private VkSurfaceFormatKHR _surfaceFormat = new()
    {
        colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR,
        format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM
    };

    private IBindlessImageFactory? _bindlessImageFactory;
    private VkCommandBuffer _transferCommandBuffer;
    private VkCommandPool _transferCommandPool;
    private VkFence _transferFence;
    private VkQueue _transferQueue;
    private uint _transferQueueFamily;

    private IntPtr _allocator;

    private unsafe void InitVulkan()
    {
        var outInstance = _instance;
        var outDevice = _device;
        var outPhysicalDevice = _physicalDevice;
        var outGraphicsQueue = _graphicsQueue;
        uint outGraphicsQueueFamily = 0;
        var outTransferQueue = _graphicsQueue;
        uint outTransferQueueFamily = 0;
        var outSurface = new VkSurfaceKHR();
        var outDebugMessenger = _debugUtilsMessenger;

        // We create a window just for surface information
        using var window = Internal_CreateWindow("Graphics Init Window", new Extent2D()) as RinWindow ??
                           throw new NullReferenceException();

        Update(0);

        Native.Vulkan.CreateInstance(window.GetHandle(),
            &outInstance,
            &outDevice,
            &outPhysicalDevice,
            &outGraphicsQueue,
            &outTransferQueueFamily,
            &outTransferQueue,
            &outTransferQueueFamily,
            &outSurface,
            &outDebugMessenger);
        _instance = outInstance;
        _device = outDevice;
        _physicalDevice = outPhysicalDevice;
        _graphicsQueue = outGraphicsQueue;
        _graphicsQueueFamily = outGraphicsQueueFamily;
        _transferQueue = outTransferQueue;
        _transferQueueFamily = outTransferQueueFamily;
        _debugUtilsMessenger = outDebugMessenger;
        _hasDedicatedTransferQueue = _graphicsQueue != _transferQueue;

        var formats = _physicalDevice.GetSurfaceFormats(outSurface).Where(c =>
        {
            //return c.format.ToString().ContainsAll("R","G","B","A","UNORM");
            var asString = c.format.ToString();
            return SurfaceFormatRegex().IsMatch(asString);
        }).ToArray();

        _surfaceFormat = formats.FirstOrDefault(_surfaceFormat);

        _descriptorAllocator = new DescriptorAllocator(10000, [
            new PoolSizeRatio(DescriptorType.StorageImage, 3),
            new PoolSizeRatio(DescriptorType.StorageBuffer, 3),
            new PoolSizeRatio(DescriptorType.UniformBuffer, 3),
            new PoolSizeRatio(DescriptorType.CombinedSamplerImage, 4)
        ]);
        _transferFence = _device.CreateFence();
        _transferCommandPool = _device.CreateCommandPool(GetTransferQueueFamily());
        _transferCommandBuffer = _device.AllocateCommandBuffers(_transferCommandPool).First();
        _graphicsFence = _device.CreateFence();
        _graphicsCommandPool = _device.CreateCommandPool(GetGraphicsQueueFamily());
        _graphicsCommandBuffer = _device.AllocateCommandBuffers(_graphicsCommandPool).First();

        _samplerFactory = new SamplerFactory(_device);
        _allocator = Native.Vulkan.CreateAllocator(_instance, _device,
            _physicalDevice);
        _shaderManager = new SlangShaderManager();
        _bindlessImageFactory = new VulkanBindlessImageFactory(this, _device);
        //_meshFactory = new MeshFactory();
        _instance.DestroySurface(outSurface);
    }

    public void Start(IApplication app)
    {
        app.OnUpdate += Update;
        app.OnCollect += Collect;
        app.OnRender += Execute;
        InitVulkan();
    }

    public void Stop(IApplication app)
    {
        app.OnRender -= Execute;
        app.OnCollect -= Collect;
        app.OnUpdate -= Update;
        
        
        lock (_renderers)
        {
            _renderers.Clear();
        }

        foreach (var (window, renderer) in _windows)
        {
            OnWindowClosed?.Invoke(window);
            OnWindowRendererDestroyed?.Invoke(renderer);
            window.Dispose();
        }

        _windows.Clear();
        _backgroundTaskQueue.Dispose();
        _transferQueueThread.Dispose();
        _descriptorAllocator?.Dispose();
        _shaderManager?.Dispose();
        _bindlessImageFactory?.Dispose();
        _meshFactory?.Dispose();
        _descriptorLayoutFactory.Dispose();

        _samplerFactory?.Dispose();
        //OnFreeRemainingMemory?.Invoke();
        
        Debug.Assert(_bufferCount == 0);
        Debug.Assert(_images.Count == 0);
        Native.Vulkan.DestroyAllocator(_allocator);
        _device.DestroyCommandPool(_graphicsCommandPool);
        _device.DestroyFence(_graphicsFence);
        _device.DestroyCommandPool(_transferCommandPool);
        _device.DestroyFence(_transferFence);
        _device.Destroy();
        if (_debugUtilsMessenger.Value != 0) Native.Vulkan.DestroyMessenger(_instance, _debugUtilsMessenger);
        _instance.Destroy();
    }

    public void Update(float deltaTime)
    {
        Native.Platform.Window.PumpEvents();

        unsafe
        {
            var events = stackalloc Native.Platform.Window.WindowEvent[_maxEventsPerPeep];
            int eventsPumped;
            do
            {
                eventsPumped = Native.Platform.Window.GetEvents(events, _maxEventsPerPeep);
                for (var i = 0; i < eventsPumped; i++)
                {
                    var e = events + i;
                    var window = _rinWindows[e->info.windowId];
                    window.ProcessEvent(*e);
                }
            } while (eventsPumped > 0);
        }
    }

    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;

    public void AddRenderer(IRenderer renderer)
    {
        throw new NotImplementedException();
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        throw new NotImplementedException();
    }

    public IWindowRenderer? GetWindowRenderer(IWindow window)
    {
        _windows.TryGetValue(window, out var windowRenderer);
        return windowRenderer;
    }

    public IRenderer[] GetRenderers()
    {
        lock (_renderers)
        {
            return _renderers.ToArray();
        }
    }

    public IWindowRenderer[] GetWindowRenderers()
    {
        lock (_windows)
        {
            return _windows.Values.ToArray();
        }
    }

    public VkSurfaceFormatKHR GetSurfaceFormat()
    {
        return _surfaceFormat;
    }

    public IGraphicsShader MakeGraphics(string path)
    {
        return _shaderManager?.MakeGraphics(path) ?? throw new NullReferenceException();
    }

    public IComputeShader MakeCompute(string path)
    {
        return _shaderManager?.MakeCompute(path) ?? throw new NullReferenceException();
    }

    public IBindlessImageFactory GetBindlessImageFactory()
    {
        return _bindlessImageFactory ?? throw new NullReferenceException();
    }

    public IMeshFactory GetMeshFactory()
    {
        return _meshFactory ?? throw new NullReferenceException();
    }

    private void HandleWindowCreated(IWindow window)
    {
        var r = new WindowRenderer(this, window);
        r.Init();
        _windows.Add(window, r);
        lock (_renderers)
        {
            _renderers.Add(r);
        }

        OnWindowCreated?.Invoke(window);
        OnWindowRendererCreated?.Invoke(r);
    }

    private void HandleWindowClosed(IWindow window)
    {
        if (!_windows.TryGetValue(window, out var window1)) return;

        lock (_renderers)
        {
            _renderers.Remove(window1);
        }

        OnWindowClosed?.Invoke(window);
        OnWindowRendererDestroyed?.Invoke(_windows[window]);
        _windows[window].Dispose();
        _windows.Remove(window);
    }

    public VkInstance GetInstance()
    {
        return _instance;
    }

    public VkDevice GetDevice()
    {
        return _device;
    }

    public VkQueue GetGraphicsQueue()
    {
        return _graphicsQueue;
    }

    public uint GetGraphicsQueueFamily()
    {
        return _graphicsQueueFamily;
    }

    public VkQueue GetTransferQueue()
    {
        return _transferQueue;
    }

    public uint GetTransferQueueFamily()
    {
        return _transferQueueFamily;
    }

    public VkPhysicalDevice GetPhysicalDevice()
    {
        return _physicalDevice;
    }


    [GeneratedRegex("(VK_FORMAT_(R|B)[0-9]{1,2}G[0-9]{1,2}(B|R)[0-9]{1,2}A[0-9]{1,2}_UNORM)")]
    private static partial Regex SurfaceFormatRegex();

    public VkDescriptorSetLayout GetDescriptorSetLayout(VkDescriptorSetLayoutCreateInfo createInfo)
    {
        return _descriptorLayoutFactory.Get(createInfo);
    }

    public VkSampler GetSampler(in SamplerSpec spec)
    {
        return _samplerFactory?.Get(spec) ?? throw new NullReferenceException();
    }


    public static VkClearColorValue MakeClearColorValue(in Vector4 color)
    {
        var clearColor = new VkClearColorValue();
        clearColor.float32[0] = color.X;
        clearColor.float32[1] = color.Y;
        clearColor.float32[2] = color.Z;
        clearColor.float32[3] = color.W;
        return clearColor;
    }

    public static VkClearDepthStencilValue MakeClearDepthStencilValue(float depth = 0.0f, uint stencil = 0)
    {
        var clearColor = new VkClearDepthStencilValue
        {
            depth = depth,
            stencil = stencil
        };
        return clearColor;
    }

    private IWindow Internal_CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.None,
        IWindow? parent = null)
    {
        var handle = Native.Platform.Window.Create(name, extent, flags);

        var win = new RinWindow(handle, parent);

        _rinWindows.Add(handle, win);

        win.OnDispose += () => { _rinWindows.Remove(handle); };

        return win;
    }

    public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
        IWindow? parent = null)
    {
        var window = Internal_CreateWindow(name, extent, flags, parent);
        window.OnDispose += () =>
        {
            HandleWindowClosed(window);
            //NativeMethods.Destroy(window.GetPtr());
        };
        HandleWindowCreated(window);
        return window;
    }

    /// <summary>
    ///     Runs an action on a background thread, useful for queue operations
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Sync(Action action)
    {
        return _backgroundTaskQueue.Enqueue(action);
    }

    public void WaitIdle()
    {
        Sync(() => { vkDeviceWaitIdle(_device); }).Wait();
    }
    
    private int _bufferCount;
    private HashSet<IVulkanImage> _images = [];

    public IVulkanDeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags,
        bool sequentialWrite = true, bool preferHost = false, bool mapped = false, string debugName = "Buffer")
    {
        unsafe
        {
            _bufferCount++;
            VkBuffer buffer = new();
            var allocation = IntPtr.Zero;

            Native.Vulkan.AllocateBuffer(&buffer, ref allocation, size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);

            return new VulkanDeviceBuffer(buffer, size, allocation, _allocator);
        }
    }


    /// <summary>
    ///     Free's a <see cref="VulkanDeviceBuffer" />
    /// </summary>
    public void FreeBuffer(VulkanDeviceBuffer buffer)
    {
        _bufferCount--;
        Native.Vulkan.FreeBuffer(buffer.NativeBuffer, buffer.Allocation, _allocator);
    }

    
    public void FreeImage(IVulkanImage image)
    {
        unsafe
        {
            _images.Remove(image);
            vkDestroyImageView(_device, image.VulkanView, null);
            Native.Vulkan.FreeImage(image.VulkanImage, image.Allocation, _allocator);
        }
    }

    public IVulkanDeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usage, bool sequentialWrite = true,
        bool mapped = true, string debugName = "Buffer")
    {
        return NewBuffer(size, usage,
            mapped
                ? VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT
                : VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, sequentialWrite, false, mapped, debugName);
    }

    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
            , sequentialWrite, true, true, debugName);
    }

    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true)
    {
        return NewBuffer(size,
            VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true);
    }

    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true)
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true);
    }

    private static VkCommandBufferBeginInfo MakeCommandBufferBeginInfo(VkCommandBufferUsageFlags flags)
    {
        return new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = flags
        };
    }

    /// <summary>
    ///     Submits using the specified queue
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="fence"></param>
    /// <param name="commandBuffers"></param>
    /// <param name="signalSemaphores"></param>
    /// <param name="waitSemaphores"></param>
    /// <returns></returns>
    public void SubmitToQueue(in VkQueue queue, in VkFence fence, VkCommandBufferSubmitInfo[] commandBuffers,
        VkSemaphoreSubmitInfo[]? signalSemaphores = null, VkSemaphoreSubmitInfo[]? waitSemaphores = null)
    {
        unsafe
        {
            var waitArr = waitSemaphores ?? [];
            var signalArr = signalSemaphores ??
                            (waitSemaphores != null ? [] : waitArr);
            fixed (VkSemaphoreSubmitInfo* pWaitSemaphores = waitArr)
            {
                fixed (VkSemaphoreSubmitInfo* pSignalSemaphores = signalArr)
                {
                    fixed (VkCommandBufferSubmitInfo* pCommandBuffers = commandBuffers)
                    {
                        var submit = new VkSubmitInfo2
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO_2,
                            pCommandBufferInfos = pCommandBuffers,
                            commandBufferInfoCount = (uint)commandBuffers.Length,
                            pSignalSemaphoreInfos = pSignalSemaphores,
                            signalSemaphoreInfoCount = (uint)signalArr.Length,
                            pWaitSemaphoreInfos = pWaitSemaphores,
                            waitSemaphoreInfoCount = (uint)waitArr.Length
                        };

                        vkQueueSubmit2(queue, 1, &submit, fence);
                    }
                }
            }
        }
    }

    public DescriptorAllocator GetDescriptorAllocator()
    {
        if (_descriptorAllocator == null) throw new Exception("How have you done this");
        return _descriptorAllocator;
    }

    public Task TransferSubmit(Action<IExecutionContext> action)
    {
        if (_hasDedicatedTransferQueue)
            return _transferQueueThread.Enqueue(() =>
            {
                unsafe
                {
                    fixed (VkFence* pFences = &_transferFence)
                    {
                        vkResetCommandBuffer(_transferCommandBuffer, 0);
                        var cmd = _transferCommandBuffer;
                        var beginInfo =
                            MakeCommandBufferBeginInfo(
                                VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                        vkBeginCommandBuffer(cmd, &beginInfo);

                        action.Invoke(new VulkanExecutionContext(_transferCommandBuffer, GetDescriptorAllocator()));

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_transferQueue, _transferFence, [
                            new VkCommandBufferSubmitInfo
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                                commandBuffer = cmd,
                                deviceMask = 0
                            }
                        ]);

                        vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                        var r = vkResetFences(_device, 1, pFences);

                        if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");
                    }
                }
            });
        lock (_pendingTransferSubmits)
        {
            var pending = new TaskCompletionSource();
            _pendingTransferSubmits.Add(new Pair<TaskCompletionSource, Action<IExecutionContext>>(pending, action));
            return pending.Task;
        }
    }

    public Task GraphicsSubmit(Action<IExecutionContext> action)
    {
        lock (_pendingGraphicsSubmits)
        {
            var pending = new TaskCompletionSource();
            _pendingGraphicsSubmits.Add(new Pair<TaskCompletionSource, Action<IExecutionContext>>(pending, action));
            return pending.Task;
        }
    }

    public VkImageView CreateImageView(VkImageViewCreateInfo createInfo)
    {
        unsafe
        {
            VkImageView view;
            vkCreateImageView(_device, &createInfo, null, &view);
            return view;
        }
    }

    private static void GenerateMipMaps(IExecutionContext ctx, IVulkanTexture image, Extent2D size, ImageFilter filter,
        ImageLayout srcLayout, ImageLayout dstLayout)
    {
        Debug.Assert(ctx is VulkanExecutionContext);
        var cmd = ((VulkanExecutionContext)ctx).CommandBuffer;
        var mipLevels = DeriveMipLevels(size);
        var curSize = size;
        for (var mip = 0; mip < mipLevels; mip++)
        {
            var halfSize = curSize;
            halfSize.Width /= 2;
            halfSize.Height /= 2;
            cmd.ImageBarrier(image, srcLayout,
                ImageLayout.TransferSrc, new ImageBarrierOptions
                {
                    SubresourceRange = new VkImageSubresourceRange
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        baseArrayLayer = 0,
                        layerCount = VK_REMAINING_ARRAY_LAYERS,
                        baseMipLevel = (uint)mip,
                        levelCount = 1
                    }
                });

            if (mip < mipLevels - 1)
            {
                var blitRegion = new VkImageBlit2
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_BLIT_2,
                    srcSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        mipLevel = (uint)mip,
                        baseArrayLayer = 0,
                        layerCount = 1
                    },
                    dstSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                        mipLevel = (uint)(mip + 1),
                        baseArrayLayer = 0,
                        layerCount = 1
                    }
                };

                blitRegion.srcOffsets[1] = new VkOffset3D
                {
                    x = (int)curSize.Width,
                    y = (int)curSize.Height,
                    z = 1
                };

                blitRegion.dstOffsets[1] = new VkOffset3D
                {
                    x = (int)halfSize.Width,
                    y = (int)halfSize.Height,
                    z = 1
                };

                unsafe
                {
                    var blitInfo = new VkBlitImageInfo2
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                        srcImage = image.VulkanImage,
                        srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                        dstImage = image.VulkanImage,
                        dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                        regionCount = 1,
                        pRegions = &blitRegion,
                        filter = filter.ToVk()
                    };

                    vkCmdBlitImage2(cmd, &blitInfo);
                }

                curSize = halfSize;
            }
        }

        cmd.ImageBarrier(image, ImageLayout.TransferSrc,
            dstLayout);
    }

    public IDisposableVulkanTexture CreateVulkanTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        unsafe
        {
            
            var extent3d = new Extent3D
            {
                Width = extent.Width,
                Height = extent.Height,
                Dimensions = 1
            };

            var imageCreateInfo = MakeImageCreateInfo(format, extent3d, usage);
            VkImage image = new();
            var allocation = IntPtr.Zero;
            Native.Vulkan.AllocateImage(ref image, ref allocation, &imageCreateInfo, _allocator, "Texture");
            var viewCreateInfo = MakeImageViewCreateInfo(format, image, format.ToAspectFlags());

            viewCreateInfo.subresourceRange.layerCount = imageCreateInfo.arrayLayers;
            viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;
            
            var vkImage = new VulkanTexture
            {
                VulkanImage = image,
                VulkanView = CreateImageView(viewCreateInfo),
                Allocation = allocation,
                Extent = extent,
                Format = format
            };

            _images.Add(vkImage);
            return vkImage;
        }
    }

    public IDisposableVulkanTextureArray CreateVulkanTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        unsafe
        {
            var extent3d = new Extent3D
            {
                Width = extent.Width,
                Height = extent.Height,
                Dimensions = 1
            };

            var imageCreateInfo = MakeImageCreateInfo(format, extent3d, usage);
            //imageCreateInfo.flags |= VkImageCreateFlags.VK_IMAGE_CREATE_2D_ARRAY_COMPATIBLE_BIT;
            imageCreateInfo.arrayLayers = count;
            imageCreateInfo.imageType = VkImageType.VK_IMAGE_TYPE_2D;
            VkImage image = new();
            var allocation = IntPtr.Zero;
            Native.Vulkan.AllocateImage(ref image, ref allocation, &imageCreateInfo, _allocator, "Texture");
            var viewCreateInfo = MakeImageViewCreateInfo(format, image, format.ToAspectFlags());
            viewCreateInfo.subresourceRange.layerCount = imageCreateInfo.arrayLayers;
            viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;

            viewCreateInfo.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D_ARRAY;

            return new VulkanTextureArray
            {
                VulkanImage = image,
                VulkanView = CreateImageView(viewCreateInfo),
                Allocation = allocation,
                Extent = extent,
                Format = format,
                Count = count
            };
        }
    }

    public IDisposableVulkanCubemap CreateVulkanCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        unsafe
        {
            var extent3d = new Extent3D
            {
                Width = extent.Width,
                Height = extent.Height,
                Dimensions = 1
            };

            var imageCreateInfo = MakeImageCreateInfo(format, extent3d, usage);
            imageCreateInfo.flags |= VkImageCreateFlags.VK_IMAGE_CREATE_CUBE_COMPATIBLE_BIT;
            imageCreateInfo.arrayLayers = 6;
            imageCreateInfo.imageType = VkImageType.VK_IMAGE_TYPE_2D;
            VkImage image = new();
            var allocation = IntPtr.Zero;
            Native.Vulkan.AllocateImage(ref image, ref allocation, &imageCreateInfo, _allocator, "Texture");
            var viewCreateInfo = MakeImageViewCreateInfo(format, image, format.ToAspectFlags());

            viewCreateInfo.subresourceRange.layerCount = imageCreateInfo.arrayLayers;
            viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;

            viewCreateInfo.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_CUBE;
            return new VulkanCubemap
            {
                VulkanImage = image,
                VulkanView = CreateImageView(viewCreateInfo),
                Allocation = allocation,
                Extent = extent,
                Format = format
            };
        }
    }

    public Task<IDisposableVulkanTexture> CreateVulkanTexture(IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format,
        bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = CreateVulkanTexture(extent, format, mips, usage | ImageUsage.TransferDst |
                                                              ImageUsage.TransferSrc);

        var dataSize = extent.Width * extent.Height * format.PixelByteSize();
        Debug.Assert(dataSize == data.ByteSize, "Unexpected image buffer size");

        var uploadBuffer = NewTransferBuffer(dataSize);
        uploadBuffer.GetView().Write(data);

        return TransferSubmit(cmd =>
        {
            cmd
                .Barrier(image, ImageLayout.Undefined, ImageLayout.TransferDst)
                .CopyToImage(uploadBuffer.GetView(), image);
        }).Then(() =>
        {
            uploadBuffer.Dispose();
            return image;
        });

        // await GraphicsSubmit(cmd =>
        // {
        //     if (mips)
        //         GenerateMipMaps(cmd, (IVulkanImage2D)newImage, extent, mipMapFilter, ImageLayout.TransferDst,
        //             ImageLayout.ShaderReadOnly);
        //     else
        //         cmd.Barrier(newImage, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
        // });

        //return image;
    }

    public Task<IDisposableVulkanTextureArray> CreateVulkanTextureArray(IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, uint count, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        throw new NotImplementedException();
    }

    public Task<IDisposableVulkanCubemap> CreateVulkanCubemap(IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format,
        bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        throw new NotImplementedException();
    }

    public IDisposableTexture CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None) => CreateVulkanTexture(extent, format, mips, usage);

    public IDisposableTextureArray CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false,
        ImageUsage usage = ImageUsage.None) => CreateVulkanTextureArray(extent, format, count, mips, usage);

    public IDisposableCubemap CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None) => CreateVulkanCubemap(extent, format, mips, usage);

    public Task<IDisposableTexture> CreateTexture(IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false,
        ImageUsage usage = ImageUsage.None) =>
        CreateVulkanTexture(data, extent, format, mips, usage).Then(IDisposableTexture (a) => a);

    public Task<IDisposableTextureArray> CreateTextureArray(IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, uint count, bool mips = false,
        ImageUsage usage = ImageUsage.None) => CreateVulkanTextureArray(data, extent, format, count, mips, usage)
        .Then(IDisposableTextureArray (a) => a);

    public Task<IDisposableCubemap> CreateCubemap(IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false,
        ImageUsage usage = ImageUsage.None) =>
        CreateVulkanCubemap(data, extent, format, mips, usage).Then(IDisposableCubemap (a) => a);

    public void CreateTexture(out ImageHandle handle, in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        handle = _bindlessImageFactory.CreateTexture(extent, format, mips, usage);
    }

    public void CreateTextureArray(out ImageHandle handle, in Extent2D extent, ImageFormat format, uint count, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        handle = _bindlessImageFactory.CreateTextureArray(extent, format,count, mips, usage);
    }

    public void CreateCubemap(out ImageHandle handle, in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        handle = _bindlessImageFactory.CreateCubemap(extent, format, mips, usage);
    }

    public Task CreateTexture(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.CreateTexture(out handle,data,extent, format, mips, usage);
    }

    public Task CreateTextureArray(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        uint count, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.CreateTextureArray(out handle,data,extent, format, count, mips, usage);
    }

    public Task CreateCubemap(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.CreateCubemap(out handle,data,extent, format, mips, usage);
    }

    public bool IsValidImageHandle(in ImageHandle handle)
    {
        return handle.Id > 0 && handle.Type switch
        {
            ImageType.Texture => GetTexture(handle) is not null,
            ImageType.Cubemap => GetCubemap(handle) is not null,
            ImageType.TextureArray => GetTextureArray(handle) is not null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public ITexture? GetTexture(in ImageHandle handle)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.GetTexture(handle);
    }

    public ITextureArray? GetTextureArray(in ImageHandle handle)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.GetTextureArray(handle);
    }

    public ICubemap? GetCubemap(in ImageHandle handle)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        return _bindlessImageFactory.GetCubemap(handle);
    }

    public void FreeImageHandles(params ImageHandle[] handles)
    {
        Debug.Assert(_bindlessImageFactory is not null);
        _bindlessImageFactory.FreeHandles(handles);
    }

    public Task CreateMesh<TVertexFormat>(out MeshHandle handle, IReadOnlyBuffer<TVertexFormat> vertices, IReadOnlyBuffer<uint> indices,
        IEnumerable<MeshSurface> surfaces) where TVertexFormat : unmanaged
    {
        throw new NotImplementedException();
    }

    public bool IsValidMeshHandle(in ImageHandle handle)
    {
        throw new NotImplementedException();
    }

    public IMesh? GetMesh(in MeshHandle handle)
    {
        throw new NotImplementedException();
    }

    public void FreeMeshHandles(params MeshHandle[] handles)
    {
        throw new NotImplementedException();
    }

    private void HandlePendingTransferSubmits()
    {
        Pair<TaskCompletionSource, Action<IExecutionContext>>[] pending;

        lock (_pendingTransferSubmits)
        {
            pending = _pendingTransferSubmits.ToArray();
            _pendingTransferSubmits.Clear();
        }

        if (pending.NotEmpty())
            unsafe
            {
                var cmd = _transferCommandBuffer;
                var ctx = new VulkanExecutionContext(cmd, GetDescriptorAllocator());
                fixed (VkFence* pFences = &_transferFence)
                {
                    vkResetCommandBuffer(cmd, 0);

                    var beginInfo =
                        MakeCommandBufferBeginInfo(
                            VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                    vkBeginCommandBuffer(cmd, &beginInfo);

                    foreach (var (_, action) in pending) action.Invoke(ctx);

                    vkEndCommandBuffer(cmd);

                    SubmitToQueue(_transferQueue, _transferFence, [
                        new VkCommandBufferSubmitInfo
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                            commandBuffer = cmd,
                            deviceMask = 0
                        }
                    ]);

                    vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                    var r = vkResetFences(_device, 1, pFences);

                    if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");

                    foreach (var (task, _) in pending) task.SetResult();
                }
            }
    }

    private void HandlePendingGraphicsSubmits()
    {
        Pair<TaskCompletionSource, Action<IExecutionContext>>[] pending;

        lock (_pendingGraphicsSubmits)
        {
            pending = _pendingGraphicsSubmits.ToArray();
            _pendingGraphicsSubmits.Clear();
        }

        if (pending.NotEmpty())
            unsafe
            {
                var ctx = new VulkanExecutionContext(_graphicsCommandBuffer, GetDescriptorAllocator());
                fixed (VkFence* pFences = &_graphicsFence)
                {
                    vkResetCommandBuffer(_graphicsCommandBuffer, 0);
                    var cmd = _graphicsCommandBuffer;
                    var beginInfo =
                        MakeCommandBufferBeginInfo(
                            VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                    vkBeginCommandBuffer(cmd, &beginInfo);

                    foreach (var (_, action) in pending) action.Invoke(ctx);

                    vkEndCommandBuffer(cmd);

                    SubmitToQueue(_graphicsQueue, _graphicsFence, [
                        new VkCommandBufferSubmitInfo
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                            commandBuffer = cmd,
                            deviceMask = 0
                        }
                    ]);

                    vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                    var r = vkResetFences(_device, 1, pFences);

                    if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");

                    foreach (var (task, _) in pending) task.SetResult();
                }
            }
    }

    public void Collect()
    {
        IRenderer[] renderers;

        lock (_renderers)
        {
            renderers = _renderers.ToArray();
        }

        List<IRenderData> collected = [];
        foreach (var renderer in renderers)
            if (renderer.Collect() is { } context)
                collected.Add(context);

        _collected = collected.ToArray();
    }

    public void Execute()
    {
        if (!_hasDedicatedTransferQueue) HandlePendingTransferSubmits();

        {
            HandlePendingGraphicsSubmits();
        }

        foreach (var context in _collected) context.Renderer.Execute(context);
    }

    public static VkRenderingInfo MakeRenderingInfo(VkExtent2D extent)
    {
        return MakeRenderingInfo(new VkRect2D
        {
            offset = new VkOffset2D
            {
                x = 0,
                y = 0
            },
            extent = extent
        });
    }

    public static VkRenderingInfo MakeRenderingInfo(VkRect2D area)
    {
        return new VkRenderingInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDERING_INFO,
            renderArea = area,
            layerCount = 1
        };
    }

    public static VkImageCreateInfo MakeImageCreateInfo(ImageFormat format, Extent3D size, ImageUsage usage)
    {
        return new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = format.ToVk(),
            extent = size.ToVk(),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            usage = usage.ToVk()
        };
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(IVulkanImage image, VkImageAspectFlags aspect)
    {
        return MakeImageViewCreateInfo(image.Format, image.VulkanImage, aspect);
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(ImageFormat format, VkImage image,
        VkImageAspectFlags aspect)
    {
        return new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = image,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = format.ToVk(),
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = aspect,
                baseMipLevel = 0,
                levelCount = VK_REMAINING_MIP_LEVELS,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };
    }


    public static VkImageSubresourceRange MakeImageSubresourceRange(VkImageAspectFlags aspectMask)
    {
        return new VkImageSubresourceRange
        {
            aspectMask = aspectMask,
            baseMipLevel = 0,
            levelCount = VK_REMAINING_MIP_LEVELS,
            baseArrayLayer = 0,
            layerCount = VK_REMAINING_ARRAY_LAYERS
        };
    }


    public static VkImageSubresourceRange MakeImageSubresourceRange(ImageFormat format)
    {
        return new VkImageSubresourceRange
        {
            aspectMask = format.ToAspectFlags(),
            baseMipLevel = 0,
            levelCount = VK_REMAINING_MIP_LEVELS,
            baseArrayLayer = 0,
            layerCount = VK_REMAINING_ARRAY_LAYERS
        };
    }

    private static uint DeriveMipLevels(Extent2D extent)
    {
        return (uint)(float.Floor(float.Log2(float.Max(extent.Width, extent.Height))) + 1);
    }

    public static VulkanGraphicsModule Get()
    {
        var inst = SFramework.Provider.Get<IGraphicsModule>();
        Debug.Assert(inst is VulkanGraphicsModule);
        return Unsafe.As<VulkanGraphicsModule>(inst);
    }
}