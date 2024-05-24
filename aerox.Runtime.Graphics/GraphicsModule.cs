using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Math;
using aerox.Runtime.Windows;
using shaderc;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     Options for image barriers
/// </summary>
public class ImageBarrierOptions
{
    public VkAccessFlags2 DstAccessFlags =
        VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT;

    public VkPipelineStageFlags2 DstStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkAccessFlags2 SrcAccessFlags = VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT;
    public VkPipelineStageFlags2 SrcStageFlags = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

    public VkImageSubresourceRange SubresourceRange =
        GraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
}

[NativeRuntimeModule(typeof(WindowsModule))]
public sealed class GraphicsModule : RuntimeModule,ISingletonGetter<GraphicsModule>, ITickable
{
    private readonly BlockingCollection<Action> _pendingQueueTasks = new();
    private readonly List<WindowRenderer> _renderers = new();
    private readonly Dictionary<string, Shader> _shaders = new();
    private readonly Dictionary<Window, WindowRenderer> _windows = new();
    private Allocator? _allocator;
    private VkDebugUtilsMessengerEXT _debugUtilsMessenger;
    private DescriptorAllocator? _descriptorAllocator;
    private VkDevice _device;
    private VkCommandBuffer _immediateCommandBuffer;
    private VkCommandPool _immediateCommandPool;
    private VkFence _immediateFence;

    private VkInstance _instance;
    private Window? _mainWindow;
    private VkPhysicalDevice _physicalDevice;
    private VkQueue _queue;
    private uint _queueFamily;
    private Task? _syncTask;
    private Thread _syncThread = Thread.CurrentThread;
    private readonly Dictionary<SamplerSpec, VkSampler> _samplers = new();
    private readonly Mutex _samplersMutex = new();


    public VkSampler GetSampler(SamplerSpec spec)
    {
        lock (_samplersMutex)
        {
            if (_samplers.TryGetValue(spec, out var sampler))
            {
                return sampler;
            }

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
                mipmapMode = VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_NEAREST,
                anisotropyEnable = 0,
                borderColor = VkBorderColor.VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK
            };

            unsafe
            {
                vkCreateSampler(GetDevice(), &samplerCreateInfo, null, &sampler);
            }
        
            _samplers.Add(spec,sampler);

            return sampler;
        }
    }
    public static VkFormat FormatToVkFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba8 => VkFormat.VK_FORMAT_R8G8B8A8_UNORM,
            ImageFormat.Rgba16 => VkFormat.VK_FORMAT_R16G16B16A16_UNORM,
            _ => VkFormat.VK_FORMAT_R8G8B8A8_UNORM
        };
    }

    public static VkFilter FilterToVkFilter(ImageFilter filter)
    {
        return filter switch
        {
            ImageFilter.Linear => VkFilter.VK_FILTER_LINEAR,
            ImageFilter.Nearest => VkFilter.VK_FILTER_NEAREST,
            ImageFilter.Cubic => VkFilter.VK_FILTER_CUBIC_IMG,
            _ => VkFilter.VK_FILTER_LINEAR
        };
    }

    public static VkSamplerAddressMode TilingToVkSamplerAddressMode(ImageTiling tiling)
    {
        return tiling switch
        {
            ImageTiling.Repeat => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT,
            ImageTiling.ClampEdge => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
            ImageTiling.ClampBorder => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_BORDER,
            _ => VkSamplerAddressMode.VK_SAMPLER_ADDRESS_MODE_REPEAT
        };
    }

    public static VkClearColorValue MakeClearColorValue(Vector4<float> color)
    {
        var clearColor = new VkClearColorValue();
        clearColor.float32[0] = 0;
        clearColor.float32[1] = 0;
        clearColor.float32[2] = 0;
        clearColor.float32[3] = 0;
        return clearColor;
    }
    
    public static GraphicsModule Get()
    {
        return Runtime.Instance.GetModule<GraphicsModule>();
    }
    
    public void Tick(double deltaSeconds)
    {
        Draw();
    }

    /// <summary>
    ///     Runs an action on a background thread, useful for queue operations
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task<T> Sync<T>(Func<T> action)
    {
        if (Thread.CurrentThread == _syncThread) return Task.FromResult(action());

        var task = new TaskCompletionSource<T>();
        _pendingQueueTasks.Add(() =>
        {
            try
            {
                task.TrySetResult(action());
            }
            catch (Exception e)
            {
                task.TrySetException(e);
            }
        });
        return task.Task;
    }

    /// <summary>
    ///     Runs an action on a background thread, useful for queue operations
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Sync(Action action)
    {
        if (Thread.CurrentThread == _syncThread)
        {
            action();
            return Task.CompletedTask;
        }

        var task = new TaskCompletionSource();
        _pendingQueueTasks.Add(() =>
        {
            try
            {
                action?.Invoke();
                task.TrySetResult();
            }
            catch (Exception e)
            {
                task.TrySetException(e);
            }
        });


        return task.Task;
    }

    private void SyncThread()
    {
        _syncThread = Thread.CurrentThread;
        foreach (var task in _pendingQueueTasks.GetConsumingEnumerable()) task?.Invoke();
    }

    public event Action<WindowRenderer> OnRendererCreated;
    public event Action<WindowRenderer> OnRendererDestroyed;

    [DllImport(Dlls.AeroxNative, EntryPoint = "windowGetExtensions", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe IntPtr NativeGetExtensions(uint* length);

    [DllImport(Dlls.AeroxNative, EntryPoint = "graphicsReflectShader", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void NativeReflectShader(void* data, uint dataSize,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeReflectionResultDelegate onReflectedDelegate);


    [DllImport(Dlls.AeroxNative, EntryPoint = "graphicsCreateVulkanInstance",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void NativeCreateInstance(IntPtr inWindow, void** outInstance, void** outDevice,
        void** outPhysicalDevice, void** outQueue, uint* outQueueFamily, ulong* outSurface, ulong* debugMessenger);

    [DllImport(Dlls.AeroxNative, EntryPoint = "graphicsDestroyVulkanMessenger",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void NativeDestroyMessenger(void* instance, ulong debugMessenger);

    public WindowRenderer? GetWindowRenderer(Window window)
    {
        _windows.TryGetValue(window, out var windowRenderer);
        return windowRenderer;
    }

    public override void Startup(Runtime runtime)
    {
        base.Startup(runtime);
        var windowSubsystem = runtime.GetModule<WindowsModule>();
        windowSubsystem.OnWindowCreated += OnWindowCreated;
        windowSubsystem.OnWindowClosed += OnWindowClosed;
        
        Runtime.Instance.OnTick += Tick;
    }

    public Window? GetMainWindow()
    {
        return _mainWindow;
    }

    public List<WindowRenderer> GetRenderers()
    {
        return _renderers;
    }

    public static uint MakeVulkanVersion(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 16) | patch;
    }

    private unsafe void InitVulkan()
    {
        //IntPtr inWindow,
        void* outInstance;
        void* outDevice;
        void* outPhysicalDevice;
        void* outQueue;
        uint outQueueFamily;
        ulong outSurface;
        VkDebugUtilsMessengerEXT outDebugMessenger;
        NativeCreateInstance(_mainWindow!.GetPtr(), &outInstance, &outDevice, &outPhysicalDevice, &outQueue,
            &outQueueFamily, &outSurface, &outDebugMessenger.Value);
        _instance = new VkInstance(outInstance);
        _device = new VkDevice(outDevice);
        _physicalDevice = new VkPhysicalDevice(outPhysicalDevice);
        _queue = new VkQueue(outQueue);
        _queueFamily = outQueueFamily;
        _debugUtilsMessenger = outDebugMessenger;

        _descriptorAllocator = new DescriptorAllocator(10000, [
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 4)
        ]);
        _allocator = new Allocator(this);

        var fenceCreateInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
            flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT
        };

        fixed (VkFence* pFence = &_immediateFence)
        {
            vkCreateFence(_device, &fenceCreateInfo, null, pFence);
        }

        var commandPoolCreateInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
            flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            queueFamilyIndex = _queueFamily
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

        _syncTask = Task.Factory.StartNew(SyncThread, TaskCreationOptions.LongRunning);
        var newRenderer = new WindowRenderer(this, _mainWindow, new VkSurfaceKHR(outSurface));
        newRenderer.Init();
        _windows.Add(_mainWindow, newRenderer);
        _renderers.Add(newRenderer);
        OnRendererCreated?.Invoke(newRenderer);

        _mainWindow.OnCloseRequested += OnCloseRequested;

        //LoadShader("D:\\Github\\vengine\\aerox.Runtime\\shaders\\2d\\rect.vert");
    }

    private static void OnCloseRequested(Window.CloseEvent e)
    {
        Runtime.Instance.RequestExit();
    }


    private unsafe Shader CompileShader(string filePath)
    {
        var opts = new Options();
        opts.EnableDebugInfo();

        using var comp = new Compiler(opts);
        using var res = comp.Compile(filePath, Shader.GetShaderStage(filePath));
        if (res.Status == Status.Success)
        {
            var ci = new VkShaderModuleCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
                codeSize = res.CodeLength,
                pCode = (uint*)res.CodePointer
            };

            var module = new VkShaderModule();
            vkCreateShaderModule(_device, &ci, null, &module);
            var jsonStr = "";
            NativeReflectShader(ci.pCode, (uint)ci.codeSize,
                (d, l) => { jsonStr = new StringBuilder(d).ToString(); });

            var shader = new Shader(module, filePath, jsonStr);
            _shaders.Add(filePath, shader);
            return shader;
        }

        throw new Exception("Error compiling shader " + res.ErrorMessage);
    }

    public Shader LoadShader(string filePath)
    {
        if (_shaders.TryGetValue(filePath, out var value) && !value.Disposed) return value;
        return CompileShader(filePath);
    }


    private void OnWindowCreated(Window window)
    {
        if (_mainWindow == null)
        {
            _mainWindow = window;
            InitVulkan();
        }
        else
        {
            var r = new WindowRenderer(this, window);
            r.Init();
            _windows.Add(window, r);
            _renderers.Add(r);
            OnRendererCreated?.Invoke(r);
        }
    }

    private void OnWindowClosed(Window window)
    {
        if (!_windows.ContainsKey(window)) return;

        if (window == _mainWindow)
        {
            GetEngine()?.RequestExit();
        }
        else
        {
            _renderers.Remove(_windows[window]);
            OnRendererDestroyed?.Invoke(_windows[window]);
            _windows[window].Dispose();
            _windows.Remove(window);
        }
    }

    public VkInstance GetInstance()
    {
        return _instance;
    }

    public VkDevice GetDevice()
    {
        return _device;
    }

    public VkQueue GetQueue()
    {
        return _queue;
    }

    public uint GetQueueFamily()
    {
        return _queueFamily;
    }

    public VkPhysicalDevice GetPhysicalDevice()
    {
        return _physicalDevice;
    }

    public Allocator GetAllocator()
    {
        return _allocator;
    }

    public DescriptorAllocator GetDescriptorAllocator()
    {
        return _descriptorAllocator;
    }

    public void WaitDeviceIdle()
    {
        Sync(() => { vkDeviceWaitIdle(_device); }).Wait();
    }

    public override void Shutdown(Runtime runtime)
    {
        base.Shutdown(runtime);
        
        var mainWindow = _mainWindow!;
        mainWindow.OnCloseRequested -= OnCloseRequested;
        _renderers.Remove(_windows[mainWindow]);
        foreach (var renderer in _renderers.ToArray()) renderer.GetWindow().Dispose();
        OnRendererDestroyed?.Invoke(_windows[mainWindow]);
        _windows[mainWindow].Dispose();
        mainWindow.Dispose();
        
        
        
        Runtime.Instance.OnTick -= Tick;
        
        _pendingQueueTasks.Add(() => _pendingQueueTasks.CompleteAdding());
        _syncTask.Wait();
        _syncTask.Dispose();
        
        _descriptorAllocator?.Dispose();
        _allocator?.Dispose();
        foreach (var shader in _shaders) shader.Value.Dispose();

        unsafe
        {
            lock (_samplersMutex)
            {
                foreach (var sampler in _samplers.Values)
                {
                    vkDestroySampler(_device,sampler,null);
                }
                
                _samplers.Clear();
            }
        }
        
        
        unsafe
        {
            vkDestroyCommandPool(_device, _immediateCommandPool, null);
            vkDestroyFence(_device, _immediateFence, null);
            vkDestroyDevice(_device, null);
            if (_debugUtilsMessenger.Value != 0) NativeDestroyMessenger(_instance, _debugUtilsMessenger);
            vkDestroyInstance(_instance, null);
        }
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
    
    
    public static void CopyImageToImage(VkCommandBuffer commandBuffer, DeviceImage src, DeviceImage dst,VkFilter filter = VkFilter.VK_FILTER_LINEAR) =>
        CopyImageToImage(commandBuffer, src.Image, dst.Image,src.Extent, dst.Extent, filter);
    
    public static void CopyImageToImage(VkCommandBuffer commandBuffer, DeviceImage src, DeviceImage dst,
        VkExtent3D srcExtent,
        VkExtent3D dstExtent, VkFilter filter = VkFilter.VK_FILTER_LINEAR) =>
        CopyImageToImage(commandBuffer, src.Image, dst.Image, srcExtent, dstExtent, filter);
    public static void CopyImageToImage(VkCommandBuffer commandBuffer, VkImage src, VkImage dst, VkExtent3D srcExtent,
        VkExtent3D dstExtent, VkFilter filter = VkFilter.VK_FILTER_LINEAR)
    {
        var blitRegion = new VkImageBlit2
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_BLIT_2,
            srcSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                baseArrayLayer = 0,
                layerCount = 1,
                mipLevel = 0
            },
            dstSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                baseArrayLayer = 0,
                layerCount = 1,
                mipLevel = 0
            }
        };

        blitRegion.srcOffsets[1] = new VkOffset3D
        {
            x = (int)srcExtent.width,
            y = (int)srcExtent.height,
            z = (int)srcExtent.depth
        };
        blitRegion.dstOffsets[1] = new VkOffset3D
        {
            x = (int)dstExtent.width,
            y = (int)dstExtent.height,
            z = (int)dstExtent.depth
        };
        unsafe
        {
            var a = blitRegion.srcOffsets[0];
            var b = blitRegion.dstOffsets[0];
            var blitInfo = new VkBlitImageInfo2
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
                srcImage = src,
                dstImage = dst,
                srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                filter = filter,
                pRegions = &blitRegion,
                regionCount = 1
            };

            vkCmdBlitImage2(commandBuffer, &blitInfo);
        }
    }

    public static VkImageCreateInfo MakeImageCreateInfo(VkFormat format, VkExtent3D size, VkImageUsageFlags usage)
    {
        return new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = format,
            extent = size,
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL,
            usage = usage
        };
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(DeviceImage image, VkImageAspectFlags aspect)
    {
        return MakeImageViewCreateInfo(image.Format, image.Image, aspect);
    }

    public static VkImageViewCreateInfo MakeImageViewCreateInfo(VkFormat format, VkImage image,
        VkImageAspectFlags aspect)
    {
        return new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            image = image,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
            format = format,
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

    public static uint DeriveMipLevels(VkExtent2D extent)
    {
        return (uint)(System.Math.Floor(System.Math.Log2(System.Math.Max(extent.width, extent.height))) + 1);
    }


    public DeviceImage CreateImage(VkExtent3D size, ImageFormat format, VkImageUsageFlags usage, bool mipMap = false,
        string debugName = "Image")
    {
        var vkFormat = FormatToVkFormat(format);
        
        var imageCreateInfo = MakeImageCreateInfo(vkFormat, size, usage);
        if (mipMap)
            imageCreateInfo.mipLevels = DeriveMipLevels(new VkExtent2D
            {
                width = size.width,
                height = size.height
            });

        var newImage = _allocator.NewDeviceImage(imageCreateInfo, debugName);

        var aspectFlags = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
        if (format == ImageFormat.D32) aspectFlags = VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT;

        var viewCreateInfo = MakeImageViewCreateInfo(vkFormat, newImage.Image, aspectFlags);

        viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;

        unsafe
        {
            fixed (VkImageView* viewPtr = &newImage.View)
            {
                vkCreateImageView(_device, &viewCreateInfo, null, viewPtr);
            }
        }

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


    public void SubmitToQueue(VkFence fence, VkCommandBufferSubmitInfo[] commandBuffers,
        VkSemaphoreSubmitInfo[]? signalSemaphores = null, VkSemaphoreSubmitInfo[]? waitSemaphores = null)
    {
        Sync(() =>
        {
            unsafe
            {
                var waitArr = waitSemaphores ?? new VkSemaphoreSubmitInfo[] { };
                var signalArr = signalSemaphores ??
                                (waitSemaphores != null ? new VkSemaphoreSubmitInfo[] { } : waitArr);
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

                            vkQueueSubmit2(_queue, 1, &submit, fence);
                        }
                    }
                }
            }
        }).Wait();
    }

    public Task ImmediateSubmitAsync(Action<VkCommandBuffer> action)
    {
        return Sync(() =>
        {
            unsafe
            {
                fixed (VkFence* pFences = &_immediateFence)
                {
                    vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                    vkResetFences(_device, 1, pFences);
                    vkResetCommandBuffer(_immediateCommandBuffer, 0);
                    var cmd = _immediateCommandBuffer;
                    var beginInfo =
                        MakeCommandBufferBeginInfo(
                            VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                    vkBeginCommandBuffer(cmd, &beginInfo);

                    action.Invoke(cmd);

                    vkEndCommandBuffer(cmd);

                    SubmitToQueue(_immediateFence, new[]
                    {
                        new VkCommandBufferSubmitInfo
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                            commandBuffer = cmd,
                            deviceMask = 0
                        }
                    });

                    vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
                }
            }
        });
    }

    public void ImmediateSubmit(Action<VkCommandBuffer> action) => ImmediateSubmitAsync(action).Wait();

    public VkImageView CreateImageView(VkImageViewCreateInfo createInfo)
    {
        unsafe
        {
            VkImageView view;
            vkCreateImageView(_device, &createInfo, null, &view);
            return view;
        }
    }

    public static void GenerateMipMaps(VkCommandBuffer cmd, DeviceImage image, VkExtent2D size, VkFilter filter)
    {
        var mipLevels = DeriveMipLevels(size);
        var curSize = size;
        for (var mip = 0; mip < mipLevels; mip++)
        {
            var halfSize = curSize;
            halfSize.width /= 2;
            halfSize.height /= 2;

            ImageBarrier(cmd, image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
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
                        srcImage = image.Image,
                        srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                        dstImage = image.Image,
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

        ImageBarrier(cmd, image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }

    public static void ImageBarrier(VkCommandBuffer commandBuffer, DeviceImage image, VkImageLayout from,
        VkImageLayout to, ImageBarrierOptions? options = null)
    {
        ImageBarrier(commandBuffer, image.Image, from, to, options);
    }


    public static void ImageBarrier(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout from,
        VkImageLayout to, ImageBarrierOptions? options = null)
    {
        var opts = options ?? new ImageBarrierOptions();
        var barrier = new VkImageMemoryBarrier2
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER_2,
            srcStageMask = opts.SrcStageFlags,
            dstStageMask = opts.DstStageFlags,
            srcAccessMask = opts.SrcAccessFlags,
            dstAccessMask = opts.DstAccessFlags,
            oldLayout = from,
            newLayout = to,
            subresourceRange = opts.SubresourceRange,
            image = image
        };

        unsafe
        {
            var depInfo = new VkDependencyInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEPENDENCY_INFO,
                imageMemoryBarrierCount = 1,
                pImageMemoryBarriers = &barrier
            };

            vkCmdPipelineBarrier2(commandBuffer, &depInfo);
        }
    }


    public DeviceImage CreateImage(byte[] data, VkExtent3D size, ImageFormat format, VkImageUsageFlags usage,
        bool mipMap = false, ImageFilter mipMapFilter = ImageFilter.Linear,
        string debugName = "Image")
    {
        if (_allocator == null) throw new Exception("Allocator is null");
        
        var dataSize = size.depth * size.width * size.height * 4;

        var uploadBuffer = _allocator.NewTransferBuffer(dataSize);
        uploadBuffer.Write(data);

        var newImage = CreateImage(size, format,
            usage | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT, mipMap, debugName);

        ImmediateSubmit(cmd =>
        {
            ImageBarrier(cmd, newImage, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
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
                vkCmdCopyBufferToImage(cmd, uploadBuffer.Buffer, newImage.Image,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, 1, &copyRegion);
            }

            if (mipMap)
                GenerateMipMaps(cmd, newImage, new VkExtent2D
                {
                    width = size.width,
                    height = size.height
                }, FilterToVkFilter(mipMapFilter));
            else
                ImageBarrier(cmd, newImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                    VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
        });
        
        uploadBuffer.Dispose();
        
        return newImage;
    }


    public void Draw()
    {
        foreach (var kv in _windows)
            if (kv.Value.ShouldDraw())
                kv.Value.Draw();
    }


    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void NativeReflectionResultDelegate(string str, uint strLength);
}