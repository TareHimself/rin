using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Windows;
using rin.Framework.Core.Extensions;
using rin.Framework.Graphics.Shaders.Slang;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics;


[Module]
public sealed partial class SGraphicsModule : IModule, ISingletonGetter<SGraphicsModule>
{
    private readonly List<WindowRenderer> _renderers = [];
    private readonly Dictionary<SamplerSpec, VkSampler> _samplers = [];
    private readonly Dictionary<IWindow, WindowRenderer> _windows = [];
    private readonly Mutex _samplersMutex = new();
    private Allocator? _allocator;
    private IShaderManager? _shaderCompiler;
    private ITextureManager? _textureManager;
    private VkDebugUtilsMessengerEXT _debugUtilsMessenger;
    private DescriptorAllocator? _descriptorAllocator;
    private VkDevice _device;
    private VkCommandBuffer _transferCommandBuffer;
    private VkCommandPool _transferCommandPool;
    private VkFence _transferFence;
    private VkCommandBuffer _graphicsCommandBuffer;
    private VkCommandPool _graphicsCommandPool;
    private VkFence _graphicsFence;
    private VkInstance _instance;
    private VkPhysicalDevice _physicalDevice;
    private VkQueue _graphicsQueue;
    private VkQueue _transferQueue;
    private uint _graphicsQueueFamily;
    private uint _transferQueueFamily;
    private bool _hasDedicatedTransferQueue = false;
    private readonly BackgroundTaskQueue _transferQueueThread = new();
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly List<Pair<TaskCompletionSource, Action<VkCommandBuffer>>> _pendingTransferSubmits = [];
    private readonly List<Pair<TaskCompletionSource, Action<VkCommandBuffer>>> _pendingGraphicsSubmits = [];
    private readonly DescriptorLayoutFactory _descriptorLayoutFactory = new();

    private VkSurfaceFormatKHR _surfaceFormat = new VkSurfaceFormatKHR()
    {
        colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR,
        format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM
    };
    
    public static readonly string
        ShadersDirectory = Path.Join(SRuntime.FrameworkAssetsDirectory,"shaders","rin");

    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<WindowRenderer>? OnRendererCreated;
    public event Action<WindowRenderer>? OnRendererDestroyed;
    
    public event Action? OnFreeRemainingMemory;

    [GeneratedRegex("VK_FORMAT_R[0-9]{1,2}G[0-9]{1,2}B[0-9]{1,2}A[0-9]{1,2}_UNORM")]
    private static partial Regex SurfaceFormatRegex();

    public static SGraphicsModule Get()
    {
        return SRuntime.Get().GetModule<SGraphicsModule>();
    }

    public VkDescriptorSetLayout GetDescriptorSetLayout(VkDescriptorSetLayoutCreateInfo createInfo) =>
        _descriptorLayoutFactory.Get(createInfo);

    public VkSampler GetSampler(SamplerSpec spec)
    {
        lock (_samplersMutex)
        {
            if (_samplers.TryGetValue(spec, out var sampler)) return sampler;

            var vkFilter = spec.Filter.ToVk();
            var vkAddressMode = spec.Tiling.ToVk();

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
    

    public static VkClearColorValue MakeClearColorValue(Vec4<float> color)
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
        return _backgroundTaskQueue.Enqueue(action);
    }


    //public Task WithGraphicsQueue(Action<VkQueue> action) => _graphicsQueueThread.Put(() => action(_graphicsQueue));


    public WindowRenderer? GetWindowRenderer(IWindow window)
    {
        _windows.TryGetValue(window, out var windowRenderer);
        return windowRenderer;
    }

    public void Startup(SRuntime runtime)
    {
        NativeMethods.InitGlfw();
        InitVulkan();
    }

    public WindowRenderer[] GetRenderers()
    {
        lock (_renderers)
        {
            return _renderers.ToArray();
        }
    }

    public VkSurfaceFormatKHR GetSurfaceFormat() => _surfaceFormat;

    public static uint MakeVulkanVersion(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 16) | patch;
    }

    private unsafe void InitVulkan()
    {

        // uint numExtensions = 0;
        // vkEnumerateInstanceExtensionProperties(null, &numExtensions, null);
        // var extensions = stackalloc VkExtensionProperties[(int)numExtensions];
        // vkEnumerateInstanceExtensionProperties(null,&numExtensions,extensions);
        // var shaderObjectExtName = System.Text.Encoding.UTF8.GetString(VK_EXT_SHADER_OBJECT_EXTENSION_NAME);
        // var supportsShaderObject = false;
        // for (var i = 0; i < numExtensions; i++)
        // {
        //
        //     var name = Marshal.PtrToStringUTF8(new IntPtr(&extensions[i].extensionName));
        //     if (name == shaderObjectExtName)
        //     {
        //         supportsShaderObject = true;
        //     }
        // }
        //
        // {
        //     var createInfo = new VkInstanceCreateInfo()
        //     {
        //         sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
        //         enabledExtensionCount = ,
        //         pNext = null,
        //     }
        // }
        // vkCreateInstance()
        
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
        using var window = Internal_CreateWindow(1, 1, "Graphics Init Window", new CreateOptions()
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
        _transferFence = _device.CreateFence();
        _transferCommandPool = _device.CreateCommandPool(GetTransferQueueFamily());
        _transferCommandBuffer = _device.AllocateCommandBuffers(_transferCommandPool).First();
        _graphicsFence = _device.CreateFence();
        _graphicsCommandPool = _device.CreateCommandPool(GetGraphicsQueueFamily());
        _graphicsCommandBuffer = _device.AllocateCommandBuffers(_graphicsCommandPool).First();
        
        _allocator = new Allocator(this);
        _shaderCompiler = new SlangShaderManager();
        _textureManager = new TextureManager();
        _instance.DestroySurface(outSurface);
    }

    public IShaderManager GetShaderManager()
    {
        return _shaderCompiler!;
    }
    
    public IGraphicsShader GraphicsShaderFromPath(string path) => GetShaderManager().GraphicsFromPath(path);
    public IComputeShader ComputeShaderFromPath(string path) => GetShaderManager().ComputeFromPath(path);

    public ITextureManager GetTextureManager()
    {
        return _textureManager!;
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
        OnRendererCreated?.Invoke(r);
    }

    private void HandleWindowClosed(IWindow window)
    {
        if (!_windows.TryGetValue(window, out var window1)) return;

        lock (_renderers)
        {
            _renderers.Remove(window1);
        }
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

    public void Shutdown(SRuntime runtime)
    {
        lock (_renderers)
        {
            _renderers.Clear();
        }
        
        foreach (var (window, renderer) in _windows)
        {
            OnWindowClosed?.Invoke(window);
            OnRendererDestroyed?.Invoke(renderer);
            renderer.Dispose();
            window.Dispose();
        }
        _windows.Clear();
        _backgroundTaskQueue.Dispose();
        _transferQueueThread.Dispose();
        _descriptorAllocator?.Dispose();
        _shaderCompiler?.Dispose();
        _textureManager?.Dispose();
        _descriptorLayoutFactory.Dispose();
        
        lock (_samplersMutex)
        {
            foreach (var sampler in _samplers.Values) _device.DestroySampler(sampler);

            _samplers.Clear();
        }
        
        OnFreeRemainingMemory?.Invoke();
        _allocator?.Dispose();
        _device.DestroyCommandPool(_graphicsCommandPool);
        _device.DestroyFence(_graphicsFence);
        _device.DestroyCommandPool(_transferCommandPool);
        _device.DestroyFence(_transferFence);
        _device.Destroy();
        if (_debugUtilsMessenger.Value != 0) NativeMethods.DestroyMessenger(_instance, _debugUtilsMessenger);
        _instance.Destroy();

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

    public static VkImageCreateInfo MakeImageCreateInfo(ImageFormat format, VkExtent3D size, VkImageUsageFlags usage)
    {
        return new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = format.ToVk(),
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

    private static uint DeriveMipLevels(VkExtent2D extent)
    {
        return (uint)(System.Math.Floor(System.Math.Log2(System.Math.Max(extent.width, extent.height))) + 1);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewTransferBuffer(int size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer")
    {
        return GetAllocator().NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
            , sequentialWrite, true, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true, string debugName = "storageBuffer")
    {
        return NewStorageBuffer(Marshal.SizeOf<T>(), sequentialWrite, debugName);
    }
    
    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewStorageBuffer(int size, bool sequentialWrite = true,
        string debugName = "Storage Buffer")
    {
        return GetAllocator().NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer(int size, bool sequentialWrite = true,
        string debugName = "Uniform Buffer")
    {
        return GetAllocator().NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer<T>(bool sequentialWrite = true, string debugName = "uniformBuffer")
    {
        return NewUniformBuffer(Marshal.SizeOf<T>(), sequentialWrite, debugName);
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
            ImageFormat.Depth => VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT,
            ImageFormat.Stencil => VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
            _ => VkImageAspectFlags
                .VK_IMAGE_ASPECT_COLOR_BIT
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

    public Task TransferSubmit(Action<VkCommandBuffer> action)
    {
        if (_hasDedicatedTransferQueue)
        {
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

                        action.Invoke(cmd);

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_transferQueue, _transferFence, new[]
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
            lock (_pendingTransferSubmits)
            {
                var pending = new TaskCompletionSource();
                _pendingTransferSubmits.Add(new Pair<TaskCompletionSource, Action<VkCommandBuffer>>(pending, action));
                return pending.Task;
            }
        }
    }

    public Task GraphicsSubmit(Action<VkCommandBuffer> action)
    {
        lock (_pendingGraphicsSubmits)
        {
            var pending = new TaskCompletionSource();
            _pendingGraphicsSubmits.Add(new Pair<TaskCompletionSource, Action<VkCommandBuffer>>(pending, action));
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

    private static void GenerateMipMaps(VkCommandBuffer cmd, IDeviceImage image, VkExtent2D size, ImageFilter filter,ImageLayout srcLayout,ImageLayout dstLayout)
    {
        var mipLevels = DeriveMipLevels(size);
        var curSize = size;
        for (var mip = 0; mip < mipLevels; mip++)
        {
            var halfSize = curSize;
            halfSize.width /= 2;
            halfSize.height /= 2;
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
                        filter = filter.ToVk()
                    };

                    vkCmdBlitImage2(cmd, &blitInfo);
                }

                curSize = halfSize;
            }
        }

        cmd.ImageBarrier(image,ImageLayout.TransferSrc,
            dstLayout);
    }


    /// <summary>
    /// Creates an image from the data in the native buffer
    /// </summary>
    /// <param name="content"></param>
    /// <param name="size"></param>
    /// <param name="format"></param>
    /// <param name="usage"></param>
    /// <param name="mips"></param>
    /// <param name="mipMapFilter"></param>
    /// <param name="debugName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<IDeviceImage> CreateImage(NativeBuffer<byte> content, VkExtent3D size, ImageFormat format,
        VkImageUsageFlags usage,
        bool mips = false, ImageFilter mipMapFilter = ImageFilter.Linear,
        string debugName = "Image")
    {
        if (_allocator == null) throw new Exception("Allocator is null");

        var dataSize = size.depth * size.width * size.height * 4;

        var uploadBuffer = NewTransferBuffer((int)dataSize);
        uploadBuffer.Write(content);

        var newImage = CreateImage(size, format,
            usage | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
            VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT, mips, debugName);

        await TransferSubmit(cmd =>
        {
            cmd.ImageBarrier(newImage,ImageLayout.Undefined,ImageLayout.TransferDst);
            
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

            cmd.CopyBufferToImage(uploadBuffer, newImage, [copyRegion]);

            if (!mips)
            {
                cmd.ImageBarrier(newImage,ImageLayout.TransferDst,ImageLayout.ShaderReadOnly);
            }
        });

        if (mips)
        {
            await GraphicsSubmit(cmd =>
            {
                GenerateMipMaps(cmd, newImage, new VkExtent2D
                {
                    width = size.width,
                    height = size.height
                }, mipMapFilter,ImageLayout.TransferDst,ImageLayout.ShaderReadOnly);
            });
        }
        
        uploadBuffer.Dispose();

        return newImage;
    }

    private void HandlePendingTransferSubmits()
    {
        Pair<TaskCompletionSource, Action<VkCommandBuffer>>[] pending;

            lock (_pendingTransferSubmits)
            {
                pending = _pendingTransferSubmits.ToArray();
                _pendingTransferSubmits.Clear();
            }

            if (pending.NotEmpty())
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

                        foreach (var (_, action) in pending)
                        {
                            action.Invoke(cmd);
                        }

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_transferQueue, _transferFence, new[]
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
    
    private void HandlePendingGraphicsSubmits()
    {
        Pair<TaskCompletionSource, Action<VkCommandBuffer>>[] pending;

            lock (_pendingGraphicsSubmits)
            {
                pending = _pendingGraphicsSubmits.ToArray();
                _pendingGraphicsSubmits.Clear();
            }

            if (pending.NotEmpty())
            {
                unsafe
                {
                    fixed (VkFence* pFences = &_graphicsFence)
                    {
                        vkResetCommandBuffer(_graphicsCommandBuffer, 0);
                        var cmd = _graphicsCommandBuffer;
                        var beginInfo =
                            MakeCommandBufferBeginInfo(
                                VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
                        vkBeginCommandBuffer(cmd, &beginInfo);

                        foreach (var (_, action) in pending)
                        {
                            action.Invoke(cmd);
                        }

                        vkEndCommandBuffer(cmd);

                        SubmitToQueue(_graphicsQueue, _graphicsFence, new[]
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
    
    
    /// <summary>
    /// Poll all windows, could be viewed as the input loop
    /// </summary>
    public void PollWindows()
    {
        NativeMethods.PollEvents();
    }

    /// <summary>
    /// Draws all <see cref="WindowRenderer"/>, could be viewed as the render loop
    /// </summary>
    public void DrawWindows()
    {
        WindowRenderer[] toDraw = [];
        lock (_renderers)
        {
            toDraw = _renderers.ToArray();
        }
        if (!_hasDedicatedTransferQueue)
        {
            HandlePendingTransferSubmits();
        }
        
        {
            HandlePendingGraphicsSubmits();
        }
        
        
        foreach (var renderer in toDraw)
            renderer.Draw();
                
    }
}