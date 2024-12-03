using System.Text.RegularExpressions;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Core.Extensions;
using rin.Framework.Graphics.Shaders.Rsl;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics;


[NativeRuntimeModule]
public sealed partial class SGraphicsModule : RuntimeModule, ISingletonGetter<SGraphicsModule>, ITickable
{
    private readonly List<WindowRenderer> _renderers = [];
    private readonly Dictionary<SamplerSpec, VkSampler> _samplers = [];
    private readonly Dictionary<IWindow, WindowRenderer> _windows = [];
    private readonly Mutex _samplersMutex = new();
    private Allocator? _allocator;
    private IShaderCompiler? _shaderCompiler;
    private ResourceManager? _resourceManager;
    private VkDebugUtilsMessengerEXT _debugUtilsMessenger;
    private DescriptorAllocator? _descriptorAllocator;
    private VkDevice _device;
    private VkCommandBuffer _immediateCommandBuffer;
    private VkCommandPool _immediateCommandPool;
    private VkFence _immediateFence;
    private VkInstance _instance;
    private VkPhysicalDevice _physicalDevice;
    private VkQueue _graphicsQueue;
    private VkQueue _transferQueue;
    private uint _graphicsQueueFamily;
    private uint _transferQueueFamily;
    private bool _hasDedicatedTransferQueue = false;
    private readonly BackgroundTaskQueue _transferQueueThread = new();
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly List<Pair<TaskCompletionSource, Action<VkCommandBuffer>>> _pendingImmediateSubmits = [];
    private readonly DescriptorLayoutFactory _descriptorLayoutFactory = new();

    private VkSurfaceFormatKHR _surfaceFormat = new VkSurfaceFormatKHR()
    {
        colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR,
        format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM
    };

    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<WindowRenderer>? OnRendererCreated;
    public event Action<WindowRenderer>? OnRendererDestroyed;

    [GeneratedRegex("VK_FORMAT_R[0-9]{1,2}G[0-9]{1,2}B[0-9]{1,2}A[0-9]{1,2}_UNORM")]
    private static partial Regex SurfaceFormatRegex();

    public static SGraphicsModule Get()
    {
        return SRuntime.Get().GetModule<SGraphicsModule>();
    }

    public void Tick(double deltaSeconds)
    {
        NativeMethods.PollEvents();
        Draw();
    }

    public VkDescriptorSetLayout GetDescriptorSetLayout(VkDescriptorSetLayoutCreateInfo createInfo) =>
        _descriptorLayoutFactory.Get(createInfo);

    public VkSampler GetSampler(SamplerSpec spec)
    {
        lock (_samplersMutex)
        {
            if (_samplers.TryGetValue(spec, out var sampler)) return sampler;

            var vkFilter = FilterToVkFilter(spec.Filter);
            var vkAddressMode = TilingToVkSamplerAddressMode(spec.Tiling);

            var samplerCreateInfo = new VkSamplerCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
                magFilter = vkFilter,
                minFilter = vkFilter,
                addressModeU = vkAddressMode,
                addressModeV = vkAddressMode,
                addressModeW = vkAddressMode,
                mipmapMode = spec.Filter switch
                {
                    ImageFilter.Linear => VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_LINEAR,
                    ImageFilter.Nearest => VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_NEAREST,
                    ImageFilter.Cubic => throw new Exception("Cubic sampler not supported"),
                    _ => throw new ArgumentOutOfRangeException()
                },
                anisotropyEnable = 0,
                borderColor = VkBorderColor.VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK
            };

            unsafe
            {
                vkCreateSampler(GetDevice(), &samplerCreateInfo, null, &sampler);
            }

            _samplers.Add(spec, sampler);

            return sampler;
        }
    }

    public static ImageFormat VkFormatToImageFormat(VkFormat format)
    {
        return format switch
        {
            VkFormat.VK_FORMAT_R8G8B8A8_UNORM => ImageFormat.Rgba8,
            VkFormat.VK_FORMAT_R16G16B16A16_UNORM => ImageFormat.Rgba16,
            VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT => ImageFormat.Rgba32,
            VkFormat.VK_FORMAT_D32_SFLOAT => ImageFormat.Depth,
            VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT => ImageFormat.Stencil,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VkFormat ImageFormatToVkFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba8 => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
            ImageFormat.Rgba16 => VkFormat.VK_FORMAT_R16G16B16A16_UNORM,
            ImageFormat.Rgba32 => VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT,
            ImageFormat.Depth => VkFormat.VK_FORMAT_D32_SFLOAT,
            ImageFormat.Stencil => VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VkFilter FilterToVkFilter(ImageFilter filter)
    {
        return filter switch
        {
            ImageFilter.Linear => VkFilter.VK_FILTER_LINEAR,
            ImageFilter.Nearest => VkFilter.VK_FILTER_NEAREST,
            ImageFilter.Cubic => VkFilter.VK_FILTER_CUBIC_IMG,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VkSamplerAddressMode TilingToVkSamplerAddressMode(ImageTiling tiling)
    {
        return tiling switch
        {
            ImageTiling.Repeat => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            ImageTiling.ClampEdge => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
            ImageTiling.ClampBorder => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_BORDER,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VkClearColorValue MakeClearColorValue(Vector4<float> color)
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

    /// <summary>
    ///     Runs an action on a background thread, useful for queue operations
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Sync(Action action)
    {
        return _backgroundTaskQueue.Put(action);
    }


    //public Task WithGraphicsQueue(Action<VkQueue> action) => _graphicsQueueThread.Put(() => action(_graphicsQueue));


    public WindowRenderer? GetWindowRenderer(IWindow window)
    {
        _windows.TryGetValue(window, out var windowRenderer);
        return windowRenderer;
    }

    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        NativeMethods.InitGlfw();
        InitVulkan();
        SRuntime.Get().OnTick += Tick;
    }

    public List<WindowRenderer> GetRenderers()
    {
        return _renderers;
    }

    public VkSurfaceFormatKHR GetSurfaceFormat() => _surfaceFormat;

    public static uint MakeVulkanVersion(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 16) | patch;
    }

    private unsafe void InitVulkan()
    {
        //IntPtr inWindow,
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
        using var window = Internal_CreateWindow(2, 2, "Graphics Init Window", new CreateOptions()
        {
            Visible = false,
            Decorated = false,
            Resizable = false
        });
        if (window == null) throw new Exception("Failed to create window to init graphics");

        NativeMethods.CreateInstance(window.GetPtr(), &outInstance, &outDevice, &outPhysicalDevice, &outGraphicsQueue,
            &outTransferQueueFamily, &outTransferQueue, &outTransferQueueFamily, &outSurface, &outDebugMessenger);
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
            var asString = c.format.ToString();
            return SurfaceFormatRegex().IsMatch(asString);
        }).ToArray();

        _surfaceFormat = formats.FirstOrDefault(_surfaceFormat);

        _descriptorAllocator = new DescriptorAllocator(10000, [
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 4)
        ]);
        _allocator = new Allocator(this);

        _shaderCompiler = new RslShaderCompiler();

        _immediateFence = _device.CreateFence();

        var commandPoolCreateInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = GetTransferQueueFamily()
        };

        fixed (VkCommandPool* pCommandPool = &_immediateCommandPool)
        {
            vkCreateCommandPool(_device, &commandPoolCreateInfo, null, pCommandPool);
        }

        var commandBufferCreateInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
            commandPool = _immediateCommandPool,
            commandBufferCount = 1,
            level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY
        };

        fixed (VkCommandBuffer* pCommandBuffer = &_immediateCommandBuffer)
        {
            vkAllocateCommandBuffers(_device, &commandBufferCreateInfo, pCommandBuffer);
        }

        _resourceManager = new ResourceManager();

        _instance.DestroySurface(outSurface);
    }

    public IShaderCompiler GetShaderCompiler()
    {
        return _shaderCompiler!;
    }

    public ResourceManager GetResourceManager()
    {
        return _resourceManager!;
    }

    private void HandleWindowCreated(IWindow window)
    {
        var r = new WindowRenderer(this, window);
        r.Init();
        _windows.Add(window, r);
        _renderers.Add(r);
        OnWindowCreated?.Invoke(window);
        OnRendererCreated?.Invoke(r);
    }

    private void HandleWindowClosed(IWindow window)
    {
        if (!_windows.TryGetValue(window, out var window1)) return;

        _renderers.Remove(window1);
        OnWindowClosed?.Invoke(window);
        OnRendererDestroyed?.Invoke(_windows[window]);
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

    public Allocator GetAllocator()
    {
        if (_allocator == null) throw new Exception("How have you done this");
        return _allocator;
    }

    public DescriptorAllocator GetDescriptorAllocator()
    {
        if (_descriptorAllocator == null) throw new Exception("How have you done this");
        return _descriptorAllocator;
    }

    private IWindow Internal_CreateWindow(int width, int height, string name, CreateOptions? options = null,
        IWindow? parent = null
    )
    {
        unsafe
        {
            var opts = options.GetValueOrDefault(new CreateOptions());
            var winPtr = NativeMethods.Create(width, height, name, &opts);
            var win = new Window(winPtr, parent);
            return win;
        }
    }

    public IWindow CreateWindow(int width, int height, string name, CreateOptions? options = null, IWindow? parent = null
    )
    {
        var window = Internal_CreateWindow(width, height, name, options, parent);
        window.OnDisposed += () =>
        {
            HandleWindowClosed(window);
            NativeMethods.Destroy(window.GetPtr());
        };
        HandleWindowCreated(window);
        return window;
    }

    public void WaitDeviceIdle()
    {
        Sync(() => { vkDeviceWaitIdle(_device); }).Wait();
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);

        foreach (var renderer in _renderers.ToArray()) renderer.Dispose();

        SRuntime.Get().OnTick -= Tick;

        _backgroundTaskQueue.Dispose();
        _transferQueueThread.Dispose();
        _descriptorAllocator?.Dispose();
        _shaderCompiler?.Dispose();
        _resourceManager?.Dispose();
        _descriptorLayoutFactory.Dispose();
        unsafe
        {
            lock (_samplersMutex)
            {
                foreach (var sampler in _samplers.Values) vkDestroySampler(_device, sampler, null);

                _samplers.Clear();
            }


            _allocator?.Dispose();
            vkDestroyCommandPool(_device, _immediateCommandPool, null);
            vkDestroyFence(_device, _immediateFence, null);
            vkDestroyDevice(_device, null);
            if (_debugUtilsMessenger.Value != 0) NativeMethods.DestroyMessenger(_instance, _debugUtilsMessenger);
            vkDestroyInstance(_instance, null);
        }

        NativeMethods.StopGlfw();
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

    public static VkRenderingAttachmentInfo MakeRenderingAttachment(VkImageView view, VkImageLayout layout,
        VkClearValue? clearValue = null)
    {
        var attachment = new VkRenderingAttachmentInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_RENDERING_ATTACHMENT_INFO,
            imageView = view,
            imageLayout = layout,
            loadOp = clearValue == null
                ? VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_LOAD
                : VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
            storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE
        };

        if (clearValue != null) attachment.clearValue = clearValue.Value;

        return attachment;
    }


    public static VkImageCreateInfo MakeImageCreateInfo(ImageFormat format, VkExtent3D size, VkImageUsageFlags usage)
    {
        return new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = ImageFormatToVkFormat(format),
            extent = size,
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            usage = usage,
        };
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(DeviceImage image, VkImageAspectFlags aspect)
    {
        return MakeImageViewCreateInfo(image.Format, image.NativeImage, aspect);
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(ImageFormat format, VkImage image,
        VkImageAspectFlags aspect)
    {
        return new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = image,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = ImageFormatToVkFormat(format),
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

    private static uint DeriveMipLevels(VkExtent2D extent)
    {
        return (uint)(System.Math.Floor(System.Math.Log2(System.Math.Max(extent.width, extent.height))) + 1);
    }


    public IDeviceImage CreateImage(VkExtent3D size, ImageFormat format, VkImageUsageFlags usage, bool mipMap = false,
        string debugName = "Image")
    {
        var imageCreateInfo = MakeImageCreateInfo(format, size, usage);
        if (mipMap)
            imageCreateInfo.mipLevels = DeriveMipLevels(new VkExtent2D
            {
                width = size.width,
                height = size.height
            });

        var newImage = GetAllocator().NewDeviceImage(imageCreateInfo, debugName);

        var aspectFlags = format switch
        {
            ImageFormat.Rgba8 or ImageFormat.Rgba16 or ImageFormat.Rgba32 => VkImageAspectFlags
                .VK_IMAGE_ASPECT_COLOR_BIT,
            ImageFormat.Depth => VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT,
            ImageFormat.Stencil => VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        var viewCreateInfo = MakeImageViewCreateInfo(format, newImage.NativeImage, aspectFlags);

        viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;

        newImage.NativeView = CreateImageView(viewCreateInfo);

        return newImage;
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
    /// Submits using the specified queue
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="fence"></param>
    /// <param name="commandBuffers"></param>
    /// <param name="signalSemaphores"></param>
    /// <param name="waitSemaphores"></param>
    /// <returns></returns>
    public void SubmitToQueue(VkQueue queue, VkFence fence, VkCommandBufferSubmitInfo[] commandBuffers,
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

    public Task ImmediateSubmit(Action<VkCommandBuffer> action)
    {
        if (_hasDedicatedTransferQueue)
        {
            return _transferQueueThread.Put(() =>
            {
                unsafe
                {
                    fixed (VkFence* pFences = &_immediateFence)
                    {
                        vkResetCommandBuffer(_immediateCommandBuffer, 0);
                        var cmd = _immediateCommandBuffer;
                        var beginInfo =
                            MakeCommandBufferBeginInfo(
                                VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                        vkBeginCommandBuffer(cmd, &beginInfo);

                        action.Invoke(cmd);

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_transferQueue, _immediateFence, new[]
                        {
                            new VkCommandBufferSubmitInfo
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                                commandBuffer = cmd,
                                deviceMask = 0
                            }
                        });

                        vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                        var r = vkResetFences(_device, 1, pFences);

                        if (r != VkResult.VK_SUCCESS)
                        {
                            throw new Exception("Failed to reset fences");
                        }
                    }
                }
            });
        }
        else
        {
            lock (_pendingImmediateSubmits)
            {
                var pending = new TaskCompletionSource();
                _pendingImmediateSubmits.Add(new Pair<TaskCompletionSource, Action<VkCommandBuffer>>(pending, action));
                return pending.Task;
            }
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

    private static void GenerateMipMaps(VkCommandBuffer cmd, IDeviceImage image, VkExtent2D size, VkFilter filter)
    {
        var mipLevels = DeriveMipLevels(size);
        var curSize = size;
        for (var mip = 0; mip < mipLevels; mip++)
        {
            var halfSize = curSize;
            halfSize.width /= 2;
            halfSize.height /= 2;

            image.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, new ImageBarrierOptions
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
                    x = (int)curSize.width,
                    y = (int)curSize.height,
                    z = 1
                };

                blitRegion.dstOffsets[1] = new VkOffset3D
                {
                    x = (int)halfSize.width,
                    y = (int)halfSize.height,
                    z = 1
                };

                unsafe
                {
                    var blitInfo = new VkBlitImageInfo2
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                        srcImage = image.NativeImage,
                        srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                        dstImage = image.NativeImage,
                        dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                        regionCount = 1,
                        pRegions = &blitRegion,
                        filter = filter
                    };

                    vkCmdBlitImage2(cmd, &blitInfo);
                }

                curSize = halfSize;
            }
        }

        cmd.ImageBarrier(image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }


    /// <summary>
    /// Creates an image from the data in the native buffer
    /// </summary>
    /// <param name="content"></param>
    /// <param name="size"></param>
    /// <param name="format"></param>
    /// <param name="usage"></param>
    /// <param name="mipMap"></param>
    /// <param name="mipMapFilter"></param>
    /// <param name="debugName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<IDeviceImage> CreateImage(NativeBuffer<byte> content, VkExtent3D size, ImageFormat format,
        VkImageUsageFlags usage,
        bool mipMap = false, ImageFilter mipMapFilter = ImageFilter.Linear,
        string debugName = "Image")
    {
        if (_allocator == null) throw new Exception("Allocator is null");

        var dataSize = size.depth * size.width * size.height * 4;

        var uploadBuffer = _allocator.NewTransferBuffer(dataSize);
        uploadBuffer.Write(content);

        var newImage = CreateImage(size, format,
            usage | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT, mipMap, debugName);

        await ImmediateSubmit(cmd =>
        {
            cmd.ImageBarrier(newImage, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
            var copyRegion = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageExtent = size
            };

            unsafe
            {
                vkCmdCopyBufferToImage(cmd, uploadBuffer.NativeBuffer, newImage.NativeImage,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);
            }

            if (mipMap)
                GenerateMipMaps(cmd, newImage, new VkExtent2D
                {
                    width = size.width,
                    height = size.height
                }, FilterToVkFilter(mipMapFilter));
            else
                cmd.ImageBarrier(newImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
        });

        uploadBuffer.Dispose();

        return newImage;
    }

    /// <summary>
    /// Draws all <see cref="WindowRenderer"/>
    /// </summary>
    private void Draw()
    {
        if (!_hasDedicatedTransferQueue)
        {
            Pair<TaskCompletionSource, Action<VkCommandBuffer>>[] pending;

            lock (_pendingImmediateSubmits)
            {
                pending = _pendingImmediateSubmits.ToArray();
                _pendingImmediateSubmits.Clear();
            }

            if (pending.NotEmpty())
            {
                unsafe
                {
                    fixed (VkFence* pFences = &_immediateFence)
                    {
                        vkResetCommandBuffer(_immediateCommandBuffer, 0);
                        var cmd = _immediateCommandBuffer;
                        var beginInfo =
                            MakeCommandBufferBeginInfo(
                                VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                        vkBeginCommandBuffer(cmd, &beginInfo);

                        foreach (var (_, action) in pending)
                        {
                            action.Invoke(cmd);
                        }

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_transferQueue, _immediateFence, new[]
                        {
                            new VkCommandBufferSubmitInfo
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                                commandBuffer = cmd,
                                deviceMask = 0
                            }
                        });

                        vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                        var r = vkResetFences(_device, 1, pFences);

                        if (r != VkResult.VK_SUCCESS)
                        {
                            throw new Exception("Failed to reset fences");
                        }

                        foreach (var (task, _) in pending)
                        {
                            task.SetResult();
                        }
                    }
                }
            }
        }
        foreach (var (_,renderer) in _windows)
            renderer.Draw();
                
    }
}