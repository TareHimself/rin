using Rin.Engine.Core;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Windows;
using SDL;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;
using static SDL.SDL3;

namespace Rin.Engine.Graphics;
    
/// <summary>
///     Handle's rendering on a <see cref="Windows" />
/// </summary>
public class WindowRenderer : IWindowRenderer
{
    private const uint FramesInFlight = 1;
    private readonly object _drawLock = new();
    private readonly SGraphicsModule _module;
    private readonly IResourcePool _resourcePool;
    private readonly HashSet<VkPresentModeKHR> _supportedPresentModes;
    private readonly VkSurfaceKHR _surface;
    private readonly IWindow _window;
    private bool _disposed;
    private Frame[] _frames = [];
    private ulong _framesRendered;
    private VkSwapchainKHR _swapchain;

    private Extent2D _swapchainExtent = new()
    {
        Width = 0,
        Height = 0
    };

    private VkImage[] _swapchainImages = [];

    private VkImageView[] _swapchainViews = [];
    private VkViewport _viewport;
    public double LastCollectElapsedTime;
    public double LastExecuteElapsedTime;

    public WindowRenderer(SGraphicsModule module, IWindow window)
    {
        _window = window;
        _surface = CreateSurface();
        _module = module;
        _supportedPresentModes = _module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();
        _resourcePool = new ResourcePool(this);
    }

    public WindowRenderer(SGraphicsModule module, IWindow window, VkSurfaceKHR surface)
    {
        _window = window;
        _surface = surface;
        _module = module;
        _supportedPresentModes = _module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();
        _resourcePool = new ResourcePool(this);
    }

    public IWindow GetWindow()
    {
        return _window;
    }

    public void Dispose()
    {
        lock (_drawLock)
        {
            _disposed = true;
            _module.WaitDeviceIdle();
            _resourcePool.Dispose();
            foreach (var frame in _frames) frame.Dispose();
            _frames = [];
            DestroySwapchain();
            unsafe
            {
                vkDestroySurfaceKHR(_module.GetInstance(), _surface, null);
            }
        }
    }

    public IRenderContext? Collect()
    {
        var start = SEngine.Get().GetTimeSeconds();
        var c = DoCollect();
        LastCollectElapsedTime = SEngine.Get().GetTimeSeconds() - start;
        return c;
    }

    public void Execute(IRenderContext context)
    {
        if (_disposed) return;

        if (context is RenderContext ctx)
        {
            if (ctx.RenderExtent.Width == 0 || ctx.RenderExtent.Height == 0) return;

            if (ctx.RenderExtent != _window.GetPixelSize()) return;

            if (_swapchainExtent != ctx.RenderExtent)
            {
                DestroySwapchain();
                if (!CreateSwapchain(ctx.RenderExtent))
                {
                    return;
                }
            }

            var start = SEngine.Get().GetTimeSeconds();
            DoExecute(ctx);
            var now = SEngine.Get().GetTimeSeconds();
            LastExecuteElapsedTime = now - start;
        }
    }

    public event Action<IGraphBuilder>? OnCollect;

    private unsafe VkSurfaceKHR CreateSurface()
    {
        var instance = SGraphicsModule.Get().GetInstance();
        var surface = new VkSurfaceKHR();
        var surfValuePtr = &surface.Value;
        // ReSharper disable once AccessToDisposedClosure
        SDL_Vulkan_CreateSurface((SDL_Window*)_window.GetPtr(), (SDL.VkInstance_T*)instance.Value, null,(SDL.VkSurfaceKHR_T**)surfValuePtr);
        //NativeMethods.CreateSurface(instance, _window.GetPtr(), &surface);
        return surface;
    }

    public uint GetNumFramesInFlight()
    {
        return FramesInFlight;
    }

    public void Init()
    {
        var windowSize = _window.GetPixelSize();
        // CreateSwapchain(new Extent2D
        // {
        //     Width = windowSize.X,
        //     Height = windowSize.Y
        // });
        InitFrames();
    }

    private unsafe bool CreateSwapchain(Extent2D extent)
    {
        var surfaceCapabilities = new VkSurfaceCapabilitiesKHR();
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_module.GetPhysicalDevice(),_surface,&surfaceCapabilities);

        if (surfaceCapabilities.minImageExtent.width > extent.Width ||
            surfaceCapabilities.minImageExtent.height > extent.Height ||
            surfaceCapabilities.maxImageExtent.width < extent.Width ||
            surfaceCapabilities.maxImageExtent.height < extent.Height)
        {
            return false;
        }
        
        _viewport.minDepth = 0.0f;
        _viewport.maxDepth = 1.0f;
        _viewport.width = extent.Width;
        _viewport.height = extent.Height;
        
        
        var device = _module.GetDevice();
        var physicalDevice = _module.GetPhysicalDevice();
        var format = _module.GetSurfaceFormat();
        var presentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR;
        // _supportedPresentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR)
        // ? VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR
        // : _supportedPresentModes.First();
        // var createInfo = new VkSwapchainCreateInfoKHR()
        // {
        //     sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
        //     
        // }
        // vkCreateSwapchainKHR()

        var createInfo = new VkSwapchainCreateInfoKHR()
        {
            sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = _surface,
            presentMode = presentMode,
            imageFormat = format.format,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            imageColorSpace = format.colorSpace,
            imageExtent = extent.ToVk(),
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            imageArrayLayers = 1,
            minImageCount = surfaceCapabilities.minImageCount + 1,
            preTransform = VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR
        };

        var swapchain = new VkSwapchainKHR();
        vkCreateSwapchainKHR(device,&createInfo, null, &swapchain);

        uint imagesCount = 0;
        vkGetSwapchainImagesKHR(device,swapchain,&imagesCount,null);
        
        _swapchainImages = new VkImage[(int)imagesCount];

        fixed (VkImage* imagesPtr = _swapchainImages)
        {
            vkGetSwapchainImagesKHR(device,swapchain,&imagesCount,imagesPtr);
        }

        _swapchainViews = _swapchainImages.Select(c =>
        {
            unsafe
            {
                var viewCreateInfo = SGraphicsModule.MakeImageViewCreateInfo(ImageFormat.Surface, c,
                    VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
                var view = new VkImageView();
                vkCreateImageView(device, &viewCreateInfo, null, &view);
                return view;
            }
        }).ToArray();
        _swapchain = swapchain;
        // vkGetSwapchainImagesKHR()
        // NativeMethods.CreateSwapchain(
        //     device,
        //     physicalDevice,
        //     _surface,
        //     (int)format.format,
        //     (int)format.colorSpace,
        //     (int)presentMode,
        //     extent.Width,
        //     extent.Height,
        //     (swapchain, swapchainImages, numSwapchainImages, swapchainImageViews, numSwapchainImageViews) =>
        //     {
        //         _swapchain = new VkSwapchainKHR(swapchain);
        //         var images = (VkImage*)swapchainImages;
        //         var imageViews = (VkImageView*)swapchainImageViews;
        //
        //         _swapchainImages = new VkImage[numSwapchainImages];
        //
        //         for (var i = 0; i < numSwapchainImages; i++) _swapchainImages[i] = images[i];
        //
        //         _swapchainViews = new VkImageView[numSwapchainImageViews];
        //
        //         for (var i = 0; i < numSwapchainImages; i++) _swapchainViews[i] = imageViews[i];
        //     });
        _swapchainExtent = extent;
        return true;
    }

    private unsafe void DestroySwapchain()
    {
        foreach (var frame in _frames) frame.WaitForLastDraw();

        if (_swapchainExtent != new Extent2D())
        {
            var device = _module.GetDevice();
            foreach (var view in _swapchainViews) vkDestroyImageView(device, view, null);
            _swapchainViews = [];
            _swapchainImages = [];


            if (_swapchain.Value != 0) vkDestroySwapchainKHR(device, _swapchain, null);
            _swapchain = new VkSwapchainKHR();
            _swapchainExtent = new Extent2D();
        }
    }

    private void InitFrames()
    {
        var frames = new List<Frame>();

        for (var i = 0; i < FramesInFlight; i++) frames.Add(new Frame(this));

        _frames = frames.ToArray();
    }

    private Frame GetCurrentFrame()
    {
        return _frames[_framesRendered % FramesInFlight];
    }

    private static void CheckResult(VkResult result)
    {
        switch (result)
        {
            case VkResult.VK_SUCCESS:
                return;
            case VkResult.VK_ERROR_OUT_OF_DATE_KHR:
            case VkResult.VK_SUBOPTIMAL_KHR:
                throw new OutOfDateException();
            default:
                throw new Exception(result.ToString());
        }
    }

    public IRenderContext? DoCollect()
    {
        if (_disposed) return null;

        var frame = GetCurrentFrame();

        var builder = new GraphBuilder();

        if (OnCollect == null || OnCollect.GetInvocationList().Length == 0) return null;

        var extent = (Extent2D)_window.GetPixelSize();

        OnCollect?.Invoke(builder);

        return new RenderContext
        {
            Renderer = this,
            TargetFrame = frame,
            GraphBuilder = builder,
            RenderExtent = extent
        };
    }


    private void DoExecute(RenderContext ctx)
    {
        lock (_drawLock)
        {
            try
            {
                var frame = ctx.TargetFrame;
                var device = _module.GetDevice();

                CheckResult(frame.WaitForLastDraw());

                _resourcePool.OnFrameStart(_framesRendered);

                frame.Reset();

                uint swapchainImageIndex = 0;

                unsafe
                {
                    CheckResult(vkAcquireNextImageKHR(device, _swapchain, ulong.MaxValue, frame.GetSwapchainSemaphore(),
                        new VkFence(),
                        &swapchainImageIndex));
                }

                var swapchainImage = new ExternalImageProxy
                {
                    Format = ImageFormat.R8,
                    Extent = new Extent3D(ctx.RenderExtent),
                    NativeImage = _swapchainImages[swapchainImageIndex],
                    NativeView = _swapchainViews[swapchainImageIndex],
                    Layout = ImageLayout.Undefined
                };

                ctx.SwapchainImageId = ctx.GraphBuilder.AddExternalImage(swapchainImage);

                var graph = ctx.GraphBuilder.Compile(_resourcePool, frame);

                var cmd = frame.GetCommandBuffer();

                vkResetCommandBuffer(cmd, 0);

                var commandBeginInfo = new VkCommandBufferBeginInfo
                {
                    sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
                    flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
                };

                unsafe
                {
                    vkBeginCommandBuffer(cmd, &commandBeginInfo);

                    cmd
                        .SetRasterizerDiscard(false)
                        .DisableMultiSampling();
                }

                if (graph != null)
                {
                    graph.Execute(frame, ctx);
                    frame.OnReset += _ => graph.Dispose();

                    cmd.ImageBarrier(swapchainImage,
                        ImageLayout.Undefined,
                        ImageLayout.TransferDst);

                    frame.DoCopy(swapchainImage);

                    cmd.ImageBarrier(swapchainImage,
                        ImageLayout.TransferDst,
                        ImageLayout.PresentSrc);
                }
                else
                {
                    cmd.ImageBarrier(swapchainImage,
                        ImageLayout.Undefined,
                        ImageLayout.PresentSrc);
                }

                vkEndCommandBuffer(cmd);

                var queue = _module.GetGraphicsQueue();

                _module.SubmitToQueue(queue, frame.GetRenderFence(), [
                        new VkCommandBufferSubmitInfo
                        {
                            sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                            deviceMask = 0,
                            commandBuffer = cmd
                        }
                    ], [
                        new VkSemaphoreSubmitInfo
                        {
                            sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                            semaphore = frame.GetRenderSemaphore(),
                            value = 1,
                            stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
                        }
                    ],
                    [
                        new VkSemaphoreSubmitInfo
                        {
                            sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                            semaphore = frame.GetSwapchainSemaphore(),
                            value = 1,
                            stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
                        }
                    ]);
                unsafe
                {
                    var renderSemaphore = frame.GetRenderSemaphore();
                    var swapchain = _swapchain;
                    var imIdx = swapchainImageIndex + 0;
                    var presentInfo = new VkPresentInfoKHR
                    {
                        sType = VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
                        pWaitSemaphores = &renderSemaphore,
                        waitSemaphoreCount = 1,
                        pSwapchains = &swapchain,
                        swapchainCount = 1,
                        pImageIndices = &imIdx
                    };

                    vkQueuePresentKHR(queue, &presentInfo);
                }

                frame.Finish();

                _framesRendered++;
            }
            catch (OutOfDateException e)
            {
                DestroySwapchain();
            }
        }
    }

    private class OutOfDateException : Exception
    {
    }

    private class RenderContext : IRenderContext
    {
        public required Frame TargetFrame { get; init; }
        public required IGraphBuilder GraphBuilder { get; init; }

        public Extent2D RenderExtent { get; init; }
        public uint SwapchainImageId { get; set; }
        public required IRenderer Renderer { get; init; }
    }
}