// using System.Diagnostics;
// using System.Numerics;
// using System.Text.RegularExpressions;
// using JetBrains.Annotations;
// using Rin.Framework.Extensions;
// using Rin.Framework.Graphics.Images;
// using Rin.Framework.Graphics.Meshes;
// using Rin.Framework.Graphics.Shaders;
// using Rin.Framework.Graphics.Vulkan;
// using Rin.Framework.Graphics.Vulkan.Descriptors;
// using Rin.Framework.Graphics.Vulkan.Meshes;
// using Rin.Framework.Graphics.Vulkan.Shaders.Slang;
// using Rin.Framework.Graphics.Windows;
// using TerraFX.Interop.Vulkan;
// using static TerraFX.Interop.Vulkan.Vulkan;
//
// namespace Rin.Framework.Graphics;
//
// [Module]
// public sealed partial class SGraphicsModule : IModule, IUpdatable, ISingletonGetter<SGraphicsModule>
// {
//    
//
//     public void Start(SApplication application)
//     {
//         
//     }
//
//     public void Stop(SApplication application)
//     {
//         
//     }
//
//     public static SGraphicsModule Get()
//     {
//         return SApplication.Get().GetModule<SGraphicsModule>();
//     }
//
//     /// <summary>
//     ///     Poll all windows, could be viewed as the input loop
//     /// </summary>
//     public void Update(float deltaTime)
//     {
//         Native.Platform.Window.PumpEvents();
//
//         unsafe
//         {
//             var events = stackalloc Native.Platform.Window.WindowEvent[_maxEventsPerPeep];
//             int eventsPumped;
//             do
//             {
//                 eventsPumped = Native.Platform.Window.GetEvents(events, _maxEventsPerPeep);
//                 for (var i = 0; i < eventsPumped; i++)
//                 {
//                     var e = events + i;
//                     var window = _rinWindows[e->info.windowId];
//                     window.ProcessEvent(*e);
//                 }
//             } while (eventsPumped > 0);
//         }
//
//         // SDL_PumpEvents();
//         //
//         // unsafe
//         // {
//         //     var events = stackalloc SDL_Event[_maxEventsPerPeep];
//         //     var eventsPumped = 0;
//         //     do
//         //     {
//         //         eventsPumped = SDL_PeepEvents(events, _maxEventsPerPeep, SDL_EventAction.SDL_GETEVENT,
//         //             (uint)SDL_EventType.SDL_EVENT_FIRST, (uint)SDL_EventType.SDL_EVENT_LAST);
//         //         for (var i = 0; i < eventsPumped; i++)
//         //         {
//         //             var e = events + i;
//         //             var windowPtr = SDL_GetWindowFromEvent(e);
//         //             if (windowPtr == null) continue;
//         //             var window = _rinWindows[(nuint)windowPtr];
//         //             window.HandleEvent(*e);
//         //         }
//         //     } while (eventsPumped == _maxEventsPerPeep);
//         // }
//     }
//
//     public event Action<IWindow>? OnWindowClosed;
//     public event Action<IWindow>? OnWindowCreated;
//     public event Action<IWindowRenderer>? OnRendererCreated;
//     public event Action<IWindowRenderer>? OnRendererDestroyed;
//
//     public event Action? OnFreeRemainingMemory;
//
//     [GeneratedRegex("(VK_FORMAT_(R|B)[0-9]{1,2}G[0-9]{1,2}(B|R)[0-9]{1,2}A[0-9]{1,2}_UNORM)")]
//     private static partial Regex SurfaceFormatRegex();
//
//     public VkDescriptorSetLayout GetDescriptorSetLayout(VkDescriptorSetLayoutCreateInfo createInfo)
//     {
//         return _descriptorLayoutFactory.Get(createInfo);
//     }
//
//     public VkSampler GetSampler(in SamplerSpec spec)
//     {
//         return _samplerFactory?.Get(spec) ?? throw new NullReferenceException();
//     }
//
//
//     public static VkClearColorValue MakeClearColorValue(Vector4 color)
//     {
//         var clearColor = new VkClearColorValue();
//         clearColor.float32[0] = color.X;
//         clearColor.float32[1] = color.Y;
//         clearColor.float32[2] = color.Z;
//         clearColor.float32[3] = color.W;
//         return clearColor;
//     }
//
//     public static VkClearDepthStencilValue MakeClearDepthStencilValue(float depth = 0.0f, uint stencil = 0)
//     {
//         var clearColor = new VkClearDepthStencilValue
//         {
//             depth = depth,
//             stencil = stencil
//         };
//         return clearColor;
//     }
//
//     /// <summary>
//     ///     Runs an action on a background thread, useful for queue operations
//     /// </summary>
//     /// <param name="action"></param>
//     /// <returns></returns>
//     public Task Sync(Action action)
//     {
//         return _backgroundTaskQueue.Enqueue(action);
//     }
//
//
//     //public Task WithGraphicsQueue(Action<VkQueue> action) => _graphicsQueueThread.Put(() => action(_graphicsQueue));
//
//
//     public IWindowRenderer? GetWindowRenderer(IWindow window)
//     {
//         _windows.TryGetValue(window, out var windowRenderer);
//         return windowRenderer;
//     }
//
//     public IRenderer[] GetRenderers()
//     {
//         lock (_renderers)
//         {
//             return _renderers.ToArray();
//         }
//     }
//
//     public IWindowRenderer[] GetWindowRenderers()
//     {
//         lock (_windows)
//         {
//             return _windows.Values.ToArray();
//         }
//     }
//
//     public VkSurfaceFormatKHR GetSurfaceFormat()
//     {
//         return _surfaceFormat;
//     }
//
//     public static uint MakeVulkanVersion(uint major, uint minor, uint patch)
//     {
//         return (major << 22) | (minor << 16) | patch;
//     }
//
//     private unsafe void InitVulkan()
//     {
//         var outInstance = _instance;
//         var outDevice = _device;
//         var outPhysicalDevice = _physicalDevice;
//         var outGraphicsQueue = _graphicsQueue;
//         uint outGraphicsQueueFamily = 0;
//         var outTransferQueue = _graphicsQueue;
//         uint outTransferQueueFamily = 0;
//         var outSurface = new VkSurfaceKHR();
//         var outDebugMessenger = _debugUtilsMessenger;
//
//         // We create a window just for surface information
//         using var window = Internal_CreateWindow("Graphics Init Window", new Extent2D()) as RinWindow ??
//                            throw new NullReferenceException();
//
//         Update(0);
//
//         Native.Vulkan.CreateInstance(window.GetHandle(),
//             &outInstance,
//             &outDevice,
//             &outPhysicalDevice,
//             &outGraphicsQueue,
//             &outTransferQueueFamily,
//             &outTransferQueue,
//             &outTransferQueueFamily,
//             &outSurface,
//             &outDebugMessenger);
//         _instance = outInstance;
//         _device = outDevice;
//         _physicalDevice = outPhysicalDevice;
//         _graphicsQueue = outGraphicsQueue;
//         _graphicsQueueFamily = outGraphicsQueueFamily;
//         _transferQueue = outTransferQueue;
//         _transferQueueFamily = outTransferQueueFamily;
//         _debugUtilsMessenger = outDebugMessenger;
//         _hasDedicatedTransferQueue = _graphicsQueue != _transferQueue;
//
//         var formats = _physicalDevice.GetSurfaceFormats(outSurface).Where(c =>
//         {
//             //return c.format.ToString().ContainsAll("R","G","B","A","UNORM");
//             var asString = c.format.ToString();
//             return SurfaceFormatRegex().IsMatch(asString);
//         }).ToArray();
//
//         _surfaceFormat = formats.FirstOrDefault(_surfaceFormat);
//
//         _descriptorAllocator = new DescriptorAllocator(10000, [
//             new PoolSizeRatio(DescriptorType.StorageImage, 3),
//             new PoolSizeRatio(DescriptorType.StorageBuffer, 3),
//             new PoolSizeRatio(DescriptorType.UniformBuffer, 3),
//             new PoolSizeRatio(DescriptorType.CombinedSamplerImage, 4)
//         ]);
//         _transferFence = _device.CreateFence();
//         _transferCommandPool = _device.CreateCommandPool(GetTransferQueueFamily());
//         _transferCommandBuffer = _device.AllocateCommandBuffers(_transferCommandPool).First();
//         _graphicsFence = _device.CreateFence();
//         _graphicsCommandPool = _device.CreateCommandPool(GetGraphicsQueueFamily());
//         _graphicsCommandBuffer = _device.AllocateCommandBuffers(_graphicsCommandPool).First();
//
//         _samplerFactory = new SamplerFactory(_device);
//         _allocator = new Allocator(this);
//         _shaderManager = new SlangShaderManager();
//         _textureFactory = new ImageFactory(_device);
//         _meshFactory = new MeshFactory();
//         _instance.DestroySurface(outSurface);
//     }
//
//     [PublicAPI]
//     public IShaderManager GetShaderManager()
//     {
//         return _shaderManager ?? throw new NullReferenceException();
//     }
//
//     public IGraphicsShader MakeGraphics(string path)
//     {
//         return GetShaderManager().MakeGraphics(path);
//     }
//
//     // public IGraphicsShader MakeGraphicsFromContent(string content)
//     // {
//     //     var path = SEngine.Get().Temp.AddStream(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
//     //     return GetShaderManager().MakeGraphics(path);
//     // }
//
//     public IComputeShader MakeCompute(string path)
//     {
//         return GetShaderManager().MakeCompute(path);
//     }
//
//     public IBindlessImageFactory GetImageFactory()
//     {
//         return _textureFactory ?? throw new NullReferenceException();
//     }
//
//     public IMeshFactory GetMeshFactory()
//     {
//         return _meshFactory ?? throw new NullReferenceException();
//     }
//
//     private void HandleWindowCreated(IWindow window)
//     {
//         var r = new WindowRenderer(this, window);
//         r.Init();
//         _windows.Add(window, r);
//         lock (_renderers)
//         {
//             _renderers.Add(r);
//         }
//
//         OnWindowCreated?.Invoke(window);
//         OnRendererCreated?.Invoke(r);
//     }
//
//     private void HandleWindowClosed(IWindow window)
//     {
//         if (!_windows.TryGetValue(window, out var window1)) return;
//
//         lock (_renderers)
//         {
//             _renderers.Remove(window1);
//         }
//
//         OnWindowClosed?.Invoke(window);
//         OnRendererDestroyed?.Invoke(_windows[window]);
//         _windows[window].Dispose();
//         _windows.Remove(window);
//     }
//
//     public VkInstance GetInstance()
//     {
//         return _instance;
//     }
//
//     public VkDevice GetDevice()
//     {
//         return _device;
//     }
//
//     public VkQueue GetGraphicsQueue()
//     {
//         return _graphicsQueue;
//     }
//
//     public uint GetGraphicsQueueFamily()
//     {
//         return _graphicsQueueFamily;
//     }
//
//     public VkQueue GetTransferQueue()
//     {
//         return _transferQueue;
//     }
//
//     public uint GetTransferQueueFamily()
//     {
//         return _transferQueueFamily;
//     }
//
//     public VkPhysicalDevice GetPhysicalDevice()
//     {
//         return _physicalDevice;
//     }
//
//     public Allocator GetAllocator()
//     {
//         return _allocator ?? throw new NullReferenceException();
//     }
//
//     public DescriptorAllocator GetDescriptorAllocator()
//     {
//         if (_descriptorAllocator == null) throw new Exception("How have you done this");
//         return _descriptorAllocator;
//     }
//
//     // private IWindow Internal_CreateWindow(int width, int height, string name, CreateOptions? options = null,
//     //     IWindow? parent = null
//     // )
//     // {
//     //     unsafe
//     //     {
//     //         var opts = options.GetValueOrDefault(new CreateOptions());
//     //         var winPtr = NativeMethods.Create(width, height, name, &opts);
//     //         var win = new Window(winPtr, parent);
//     //         return win;
//     //     }
//     // }
//
//     private IWindow Internal_CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.None,
//         IWindow? parent = null)
//     {
//         var handle = Native.Platform.Window.Create(name, extent, flags);
//
//         var win = new RinWindow(handle, parent);
//
//         _rinWindows.Add(handle, win);
//
//         win.OnDispose += () => { _rinWindows.Remove(handle); };
//
//         return win;
//     }
//
//     public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
//         IWindow? parent = null)
//     {
//         var window = Internal_CreateWindow(name, extent, flags, parent);
//         window.OnDispose += () =>
//         {
//             HandleWindowClosed(window);
//             //NativeMethods.Destroy(window.GetPtr());
//         };
//         HandleWindowCreated(window);
//         return window;
//     }
//
//     public void WaitDeviceIdle()
//     {
//         Sync(() => { vkDeviceWaitIdle(_device); }).Wait();
//     }
//
//     
//
//     /// <summary>
//     ///     Allocates a <see cref="VulkanDeviceBuffer" /> for transfers/staging
//     /// </summary>
//     public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
//         string debugName = "Transfer Buffer")
//     {
//         return GetAllocator().NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
//             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
//             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
//             , sequentialWrite, true, true, debugName);
//     }
//
//     /// <summary>
//     ///     Allocates a <see cref="VulkanDeviceBuffer" /> for transfers/staging
//     /// </summary>
//     public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true, string debugName = "storageBuffer")
//         where T : unmanaged
//     {
//         return NewStorageBuffer(Utils.ByteSizeOf<T>(), sequentialWrite, debugName);
//     }
//
//     /// <summary>
//     ///     Allocates a <see cref="VulkanDeviceBuffer" /> for shader uniforms
//     /// </summary>
//     public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true,
//         string debugName = "Storage Buffer")
//     {
//         return GetAllocator().NewBuffer(size,
//             VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
//             VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
//             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
//     }
//
//     public IDeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usage, bool sequentialWrite = true,
//         bool mapped = true, string debugName = "Buffer")
//     {
//         return GetAllocator().NewBuffer(size, usage,
//             mapped
//                 ? VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT
//                 : VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, sequentialWrite, false, mapped, debugName);
//     }
//
//     /// <summary>
//     ///     Allocates a <see cref="VulkanDeviceBuffer" /> for shader uniforms
//     /// </summary>
//     public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true,
//         string debugName = "Uniform Buffer")
//     {
//         return GetAllocator().NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
//             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
//     }
//
//     /// <summary>
//     ///     Allocates a <see cref="VulkanDeviceBuffer" /> for shader uniforms
//     /// </summary>
//     public IDeviceBuffer NewUniformBuffer<T>(bool sequentialWrite = true, string debugName = "uniformBuffer")
//         where T : unmanaged
//     {
//         return NewUniformBuffer(Utils.ByteSizeOf<T>(), sequentialWrite, debugName);
//     }
//
//     public IImage2D CreateDeviceImage(Extent3D size, ImageFormat format, ImageUsage usage, bool mips = false,
//         string debugName = "Image")
//     {
//         
//         var imageCreateInfo = MakeImageCreateInfo(format, size, usage);
//
//         if (mips)
//             imageCreateInfo.mipLevels = DeriveMipLevels(size);
//
//         var newImage = (VulkanDeviceImage)GetAllocator().NewDeviceImage(imageCreateInfo, debugName);
//
//         var aspectFlags = format switch
//         {
//             ImageFormat.Depth => VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT,
//             ImageFormat.Stencil => VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
//             _ => VkImageAspectFlags
//                 .VK_IMAGE_ASPECT_COLOR_BIT
//         };
//
//         var viewCreateInfo = MakeImageViewCreateInfo(format, newImage.NativeImage, aspectFlags);
//
//         viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;
//
//         newImage.NativeView = CreateImageView(viewCreateInfo);
//
//         return newImage;
//     }
//
//     private static VkCommandBufferBeginInfo MakeCommandBufferBeginInfo(VkCommandBufferUsageFlags flags)
//     {
//         return new VkCommandBufferBeginInfo
//         {
//             sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
//             flags = flags
//         };
//     }
//
//     /// <summary>
//     ///     Submits using the specified queue
//     /// </summary>
//     /// <param name="queue"></param>
//     /// <param name="fence"></param>
//     /// <param name="commandBuffers"></param>
//     /// <param name="signalSemaphores"></param>
//     /// <param name="waitSemaphores"></param>
//     /// <returns></returns>
//     public void SubmitToQueue(in VkQueue queue, in VkFence fence, VkCommandBufferSubmitInfo[] commandBuffers,
//         VkSemaphoreSubmitInfo[]? signalSemaphores = null, VkSemaphoreSubmitInfo[]? waitSemaphores = null)
//     {
//         unsafe
//         {
//             var waitArr = waitSemaphores ?? [];
//             var signalArr = signalSemaphores ??
//                             (waitSemaphores != null ? [] : waitArr);
//             fixed (VkSemaphoreSubmitInfo* pWaitSemaphores = waitArr)
//             {
//                 fixed (VkSemaphoreSubmitInfo* pSignalSemaphores = signalArr)
//                 {
//                     fixed (VkCommandBufferSubmitInfo* pCommandBuffers = commandBuffers)
//                     {
//                         var submit = new VkSubmitInfo2
//                         {
//                             sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO_2,
//                             pCommandBufferInfos = pCommandBuffers,
//                             commandBufferInfoCount = (uint)commandBuffers.Length,
//                             pSignalSemaphoreInfos = pSignalSemaphores,
//                             signalSemaphoreInfoCount = (uint)signalArr.Length,
//                             pWaitSemaphoreInfos = pWaitSemaphores,
//                             waitSemaphoreInfoCount = (uint)waitArr.Length
//                         };
//
//                         vkQueueSubmit2(queue, 1, &submit, fence);
//                     }
//                 }
//             }
//         }
//     }
//
//     public Task TransferSubmit(Action<IExecutionContext> action)
//     {
//         if (_hasDedicatedTransferQueue)
//             return _transferQueueThread.Enqueue(() =>
//             {
//                 unsafe
//                 {
//                     fixed (VkFence* pFences = &_transferFence)
//                     {
//                         vkResetCommandBuffer(_transferCommandBuffer, 0);
//                         var cmd = _transferCommandBuffer;
//                         var beginInfo =
//                             MakeCommandBufferBeginInfo(
//                                 VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
//                         vkBeginCommandBuffer(cmd, &beginInfo);
//
//                         action.Invoke(new VulkanExecutionContext(_transferCommandBuffer, GetDescriptorAllocator()));
//
//                         vkEndCommandBuffer(cmd);
//
//                         SubmitToQueue(_transferQueue, _transferFence, [
//                             new VkCommandBufferSubmitInfo
//                             {
//                                 sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
//                                 commandBuffer = cmd,
//                                 deviceMask = 0
//                             }
//                         ]);
//
//                         vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
//                         var r = vkResetFences(_device, 1, pFences);
//
//                         if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");
//                     }
//                 }
//             });
//         lock (_pendingTransferSubmits)
//         {
//             var pending = new TaskCompletionSource();
//             _pendingTransferSubmits.Add(new Pair<TaskCompletionSource, Action<IExecutionContext>>(pending, action));
//             return pending.Task;
//         }
//     }
//
//     public Task GraphicsSubmit(Action<IExecutionContext> action)
//     {
//         lock (_pendingGraphicsSubmits)
//         {
//             var pending = new TaskCompletionSource();
//             _pendingGraphicsSubmits.Add(new Pair<TaskCompletionSource, Action<IExecutionContext>>(pending, action));
//             return pending.Task;
//         }
//     }
//
//     public VkImageView CreateImageView(VkImageViewCreateInfo createInfo)
//     {
//         unsafe
//         {
//             VkImageView view;
//             vkCreateImageView(_device, &createInfo, null, &view);
//             return view;
//         }
//     }
//
//     private static void GenerateMipMaps(IExecutionContext ctx, IVulkanImage2D image, Extent2D size, ImageFilter filter,
//         ImageLayout srcLayout, ImageLayout dstLayout)
//     {
//         Debug.Assert(ctx is VulkanExecutionContext);
//         var cmd = ((VulkanExecutionContext)ctx).CommandBuffer;
//         var mipLevels = DeriveMipLevels(size);
//         var curSize = size;
//         for (var mip = 0; mip < mipLevels; mip++)
//         {
//             var halfSize = curSize;
//             halfSize.Width /= 2;
//             halfSize.Height /= 2;
//             cmd.ImageBarrier(image, srcLayout,
//                 ImageLayout.TransferSrc, new ImageBarrierOptions
//                 {
//                     SubresourceRange = new VkImageSubresourceRange
//                     {
//                         aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
//                         baseArrayLayer = 0,
//                         layerCount = VK_REMAINING_ARRAY_LAYERS,
//                         baseMipLevel = (uint)mip,
//                         levelCount = 1
//                     }
//                 });
//
//             if (mip < mipLevels - 1)
//             {
//                 var blitRegion = new VkImageBlit2
//                 {
//                     sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_BLIT_2,
//                     srcSubresource = new VkImageSubresourceLayers
//                     {
//                         aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
//                         mipLevel = (uint)mip,
//                         baseArrayLayer = 0,
//                         layerCount = 1
//                     },
//                     dstSubresource = new VkImageSubresourceLayers
//                     {
//                         aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
//                         mipLevel = (uint)(mip + 1),
//                         baseArrayLayer = 0,
//                         layerCount = 1
//                     }
//                 };
//
//                 blitRegion.srcOffsets[1] = new VkOffset3D
//                 {
//                     x = (int)curSize.Width,
//                     y = (int)curSize.Height,
//                     z = 1
//                 };
//
//                 blitRegion.dstOffsets[1] = new VkOffset3D
//                 {
//                     x = (int)halfSize.Width,
//                     y = (int)halfSize.Height,
//                     z = 1
//                 };
//
//                 unsafe
//                 {
//                     var blitInfo = new VkBlitImageInfo2
//                     {
//                         sType = VkStructureType.VK_STRUCTURE_TYPE_BLIT_IMAGE_INFO_2,
//                         srcImage = image.NativeImage,
//                         srcImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
//                         dstImage = image.NativeImage,
//                         dstImageLayout = VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
//                         regionCount = 1,
//                         pRegions = &blitRegion,
//                         filter = filter.ToVk()
//                     };
//
//                     vkCmdBlitImage2(cmd, &blitInfo);
//                 }
//
//                 curSize = halfSize;
//             }
//         }
//
//         cmd.ImageBarrier(image, ImageLayout.TransferSrc,
//             dstLayout);
//     }
//
//
//     public async Task<IImage2D> CreateDeviceImage(IHostImage image, ImageUsage usage, bool mips = false,
//         ImageFilter mipMapFilter = ImageFilter.Linear, string debugName = "Image")
//     {
//         Debug.Assert(_allocator != null, "Allocator cannot be null");
//         var extent = new Extent3D(image.Extent);
//         var format = image.Format.ToDeviceFormat();
//         var dataSize =
//             extent.Width * extent.Height * format.PixelByteSize(); //size.depth * size.width * size.height * 4;
//         using var buffer = image.ToBuffer();
//         Debug.Assert(dataSize == buffer.GetByteSize(), "Unexpected image buffer size");
//
//         var uploadBuffer = NewTransferBuffer(dataSize);
//         uploadBuffer.GetView().Write(buffer);
//         var newImage = CreateDeviceImage(extent, format,
//             usage | ImageUsage.TransferDst |
//             ImageUsage.TransferSrc, mips, debugName);
//
//         await TransferSubmit(cmd =>
//         {
//             cmd
//                 .Barrier(newImage, ImageLayout.Undefined, ImageLayout.TransferDst)
//                 .CopyToImage(uploadBuffer.GetView(), newImage);
//         });
//
//         await GraphicsSubmit(cmd =>
//         {
//             if (mips)
//                 GenerateMipMaps(cmd, (IVulkanImage2D)newImage, extent, mipMapFilter, ImageLayout.TransferDst,
//                     ImageLayout.ShaderReadOnly);
//             else
//                 cmd.Barrier(newImage, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
//         });
//
//         uploadBuffer.Dispose();
//
//         return newImage;
//     }
//
//     /// <summary>
//     ///     Creates an image from the data in the native buffer
//     /// </summary>
//     /// <param name="content"></param>
//     /// <param name="size"></param>
//     /// <param name="format"></param>
//     /// <param name="usage"></param>
//     /// <param name="mips"></param>
//     /// <param name="mipsGenerateFilter"></param>
//     /// <param name="debugName"></param>
//     /// <returns></returns>
//     /// <exception cref="Exception"></exception>
//     public async Task<IImage2D> CreateDeviceImage(Buffer<byte> content, Extent3D size, ImageFormat format,
//         ImageUsage usage,
//         bool mips = false, ImageFilter mipsGenerateFilter = ImageFilter.Linear,
//         string debugName = "Image")
//     {
//         if (_allocator == null) throw new Exception("Allocator is null");
//
//         var dataSize = size.Width * size.Height * format.PixelByteSize(); //size.depth * size.width * size.height * 4;
//         var contentSize = content.GetByteSize();
//         if (dataSize != contentSize)
//             throw new Exception($"computed data size {dataSize} is not equal to content size {contentSize}");
//
//         var uploadBuffer = NewTransferBuffer(dataSize);
//         uploadBuffer.GetView().Write(content);
//
//         var newImage = CreateDeviceImage(size, format,
//             usage | ImageUsage.TransferSrc |
//             ImageUsage.TransferDst, mips, debugName);
//
//
//         if (_hasDedicatedTransferQueue)
//         {
//             await TransferSubmit(cmd =>
//             {
//                 cmd
//                     .Barrier(newImage, ImageLayout.Undefined, ImageLayout.TransferDst)
//                     .CopyToImage(uploadBuffer.GetView(), newImage);
//             });
//
//             await GraphicsSubmit(cmd =>
//             {
//                 if (mips)
//                     GenerateMipMaps(cmd, (IVulkanImage2D)newImage, size, mipsGenerateFilter, ImageLayout.TransferDst,
//                         ImageLayout.ShaderReadOnly);
//                 else
//                     cmd.Barrier(newImage, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
//             });
//         }
//         else
//         {
//             await GraphicsSubmit(cmd =>
//             {
//                 cmd
//                     .Barrier(newImage, ImageLayout.Undefined, ImageLayout.TransferDst)
//                     .CopyToImage(uploadBuffer.GetView(), newImage);
//
//                 if (mips)
//                     GenerateMipMaps(cmd, (IVulkanImage2D)newImage, size, mipsGenerateFilter, ImageLayout.TransferDst,
//                         ImageLayout.ShaderReadOnly);
//                 else
//                     cmd.Barrier(newImage, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
//             });
//         }
//
//
//         uploadBuffer.Dispose();
//
//         return newImage;
//     }
//
//     private void HandlePendingTransferSubmits()
//     {
//         Pair<TaskCompletionSource, Action<IExecutionContext>>[] pending;
//
//         lock (_pendingTransferSubmits)
//         {
//             pending = _pendingTransferSubmits.ToArray();
//             _pendingTransferSubmits.Clear();
//         }
//
//         if (pending.NotEmpty())
//             unsafe
//             {
//                 var cmd = _transferCommandBuffer;
//                 var ctx = new VulkanExecutionContext(cmd, GetDescriptorAllocator());
//                 fixed (VkFence* pFences = &_transferFence)
//                 {
//                     vkResetCommandBuffer(cmd, 0);
//
//                     var beginInfo =
//                         MakeCommandBufferBeginInfo(
//                             VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
//                     vkBeginCommandBuffer(cmd, &beginInfo);
//
//                     foreach (var (_, action) in pending) action.Invoke(ctx);
//
//                     vkEndCommandBuffer(cmd);
//
//                     SubmitToQueue(_transferQueue, _transferFence, [
//                         new VkCommandBufferSubmitInfo
//                         {
//                             sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
//                             commandBuffer = cmd,
//                             deviceMask = 0
//                         }
//                     ]);
//
//                     vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
//                     var r = vkResetFences(_device, 1, pFences);
//
//                     if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");
//
//                     foreach (var (task, _) in pending) task.SetResult();
//                 }
//             }
//     }
//
//     private void HandlePendingGraphicsSubmits()
//     {
//         Pair<TaskCompletionSource, Action<IExecutionContext>>[] pending;
//
//         lock (_pendingGraphicsSubmits)
//         {
//             pending = _pendingGraphicsSubmits.ToArray();
//             _pendingGraphicsSubmits.Clear();
//         }
//
//         if (pending.NotEmpty())
//             unsafe
//             {
//                 var ctx = new VulkanExecutionContext(_graphicsCommandBuffer, GetDescriptorAllocator());
//                 fixed (VkFence* pFences = &_graphicsFence)
//                 {
//                     vkResetCommandBuffer(_graphicsCommandBuffer, 0);
//                     var cmd = _graphicsCommandBuffer;
//                     var beginInfo =
//                         MakeCommandBufferBeginInfo(
//                             VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT);
//                     vkBeginCommandBuffer(cmd, &beginInfo);
//
//                     foreach (var (_, action) in pending) action.Invoke(ctx);
//
//                     vkEndCommandBuffer(cmd);
//
//                     SubmitToQueue(_graphicsQueue, _graphicsFence, [
//                         new VkCommandBufferSubmitInfo
//                         {
//                             sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
//                             commandBuffer = cmd,
//                             deviceMask = 0
//                         }
//                     ]);
//
//                     vkWaitForFences(_device, 1, pFences, 1, ulong.MaxValue);
//                     var r = vkResetFences(_device, 1, pFences);
//
//                     if (r != VkResult.VK_SUCCESS) throw new Exception("Failed to reset fences");
//
//                     foreach (var (task, _) in pending) task.SetResult();
//                 }
//             }
//     }
//
//
//     /// <summary>
//     ///     Call <see cref="IRenderer.Collect" /> for each <see cref="IRenderer" />
//     /// </summary>
//     public void Collect()
//     {
//         IRenderer[] renderers;
//
//         lock (_renderers)
//         {
//             renderers = _renderers.ToArray();
//         }
//
//         List<IRenderData> collected = [];
//         foreach (var renderer in renderers)
//             if (renderer.Collect() is { } context)
//                 collected.Add(context);
//
//         _collected = collected.ToArray();
//     }
//
//     /// <summary>
//     ///     Resolve pending transfer submits and call <see cref="IRenderer.Execute" /> on all collected
//     ///     <see cref="IRenderer" />
//     /// </summary>
//     public void Execute()
//     {
//         if (!_hasDedicatedTransferQueue) HandlePendingTransferSubmits();
//
//         {
//             HandlePendingGraphicsSubmits();
//         }
//
//         foreach (var context in _collected) context.Renderer.Execute(context);
//     }
// }