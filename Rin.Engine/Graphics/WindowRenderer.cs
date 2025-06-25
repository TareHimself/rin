using System.Diagnostics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Windows;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     Handle's rendering on a <see cref="Windows" />
/// </summary>
public class WindowRenderer : IWindowRenderer
{
    private const uint FramesInFlight = 1;
    private readonly Lock _drawLock = new();
    private readonly SGraphicsModule _module;
    private readonly IResourcePool _resourcePool;
    private readonly HashSet<VkPresentModeKHR> _supportedPresentModes;
    private readonly VkSurfaceKHR _surface;
    private readonly TaskPool _taskPool = new();
    private readonly IWindow _window;
    private bool _disposed;
    private Frame[] _frames = [];
    private ulong _framesRendered;
    private VkSemaphore[] _renderSemaphores = [];
    private VkSwapchainKHR _swapchain;

    private Extent2D _swapchainExtent = new()
    {
        Width = 0,
        Height = 0
    };

    private VkImage[] _swapchainImages = [];

    private VkImageView[] _swapchainViews = [];
    public double LastCollectElapsedTime;
    public double LastExecuteElapsedTime;


    public WindowRenderer(SGraphicsModule module, IWindow window)
    {
        _module = module;
        _window = window;
        _surface = CreateSurface();
        _supportedPresentModes = module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();
        _resourcePool = new ResourcePool(this);
    }

    public WindowRenderer(SGraphicsModule module, IWindow window, VkSurfaceKHR surface)
    {
        _module = module;
        _window = window;
        _surface = surface;
        _supportedPresentModes = module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();
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

    public IRenderData? Collect()
    {
        var start = SEngine.Get().GetTimeSeconds();
        var c = DoCollect();
        LastCollectElapsedTime = SEngine.Get().GetTimeSeconds() - start;
        return c;
    }

    public void Execute(IRenderData context)
    {
        if (_disposed) return;

        if (context is RenderData ctx)
        {
            if (ctx.RenderExtent.Width == 0 || ctx.RenderExtent.Height == 0) return;

            if (ctx.RenderExtent != _window.GetSize()) return;

            if (_swapchainExtent != ctx.RenderExtent)
            {
                DestroySwapchain();
                if (!CreateSwapchain(ctx.RenderExtent)) return;
            }

            var start = SEngine.Get().GetTimeSeconds();
            DoExecute(ctx);
            var now = SEngine.Get().GetTimeSeconds();
            LastExecuteElapsedTime = now - start;
        }
    }

    public event Action<IGraphBuilder>? OnCollect;

    private VkSurfaceKHR CreateSurface()
    {
        var instance = SGraphicsModule.Get().GetInstance();
        var rinWindow = _window as RinWindow ?? throw new NullReferenceException();
        // ReSharper disable once AccessToDisposedClosure
        var surface = Native.Platform.Window.CreateSurface(instance, rinWindow.GetHandle());
        return surface;
    }

    public uint GetNumFramesInFlight()
    {
        return FramesInFlight;
    }

    public void Init()
    {
        var windowSize = _window.GetSize();
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
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_module.GetPhysicalDevice(), _surface, &surfaceCapabilities);

        if (surfaceCapabilities.minImageExtent.width > extent.Width ||
            surfaceCapabilities.minImageExtent.height > extent.Height ||
            surfaceCapabilities.maxImageExtent.width < extent.Width ||
            surfaceCapabilities.maxImageExtent.height < extent.Height)
            return false;

        var device = _module.GetDevice();
        var format = _module.GetSurfaceFormat();
        var presentMode = _supportedPresentModes.First();
        var createInfo = new VkSwapchainCreateInfoKHR
        {
            sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = _surface,
            presentMode = presentMode,
            imageFormat = format.format,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            imageColorSpace = format.colorSpace,
            imageExtent = extent.ToVk(),
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                         VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            imageArrayLayers = 1,
            minImageCount = surfaceCapabilities.minImageCount + 1,
            preTransform = VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR
        };

        var swapchain = new VkSwapchainKHR();
        vkCreateSwapchainKHR(device, &createInfo, null, &swapchain);

        uint imagesCount = 0;
        vkGetSwapchainImagesKHR(device, swapchain, &imagesCount, null);

        _swapchainImages = new VkImage[(int)imagesCount];
        _renderSemaphores = Enumerable.Range(0, (int)imagesCount).Select(_ => device.CreateSemaphore()).ToArray();

        fixed (VkImage* imagesPtr = _swapchainImages)
        {
            vkGetSwapchainImagesKHR(device, swapchain, &imagesCount, imagesPtr);
        }

        _swapchainViews = _swapchainImages.Select(c =>
        {
            var viewCreateInfo = SGraphicsModule.MakeImageViewCreateInfo(ImageFormat.Swapchain, c,
                VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            var view = new VkImageView();
            vkCreateImageView(device, &viewCreateInfo, null, &view);
            return view;
        }).ToArray();
        _swapchain = swapchain;
        _swapchainExtent = extent;
        return true;
    }

    private unsafe void DestroySwapchain()
    {
        SGraphicsModule.Get().WaitDeviceIdle();
        foreach (var frame in _frames) frame.WaitForLastDraw();

        if (_swapchainExtent != new Extent2D())
        {
            var device = _module.GetDevice();

            foreach (var renderSemaphore in _renderSemaphores) device.DestroySemaphore(renderSemaphore);
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
            case VkResult.VK_ERROR_DEVICE_LOST:
            {
                Console.WriteLine("GPU Device Lost");
                Environment.Exit(1);
            }
                break;
            default:
                throw new Exception(result.ToString());
        }
    }

    public IRenderData? DoCollect()
    {
        if (_disposed) return null;

        var frame = GetCurrentFrame();

        var builder = new GraphBuilder();

        if (OnCollect == null || OnCollect.GetInvocationList().Length == 0) return null;

        var extent = _window.GetSize();

        OnCollect?.Invoke(builder);

        builder.AddPass(new PrepareForPresentPass()); // Always terminal

        return new RenderData
        {
            Renderer = this,
            TargetFrame = frame,
            GraphBuilder = builder,
            RenderExtent = extent
        };
    }


    private void DoExecute(RenderData ctx)
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

                var swapchainImage = new SwapchainImage
                {
                    Format = ImageFormat.Swapchain,
                    Extent = new Extent3D(ctx.RenderExtent),
                    NativeImage = _swapchainImages[swapchainImageIndex],
                    NativeView = _swapchainViews[swapchainImageIndex]
                };

                ctx.SwapchainImageId = ctx.GraphBuilder.AddSwapchainImage(swapchainImage);

                Profiling.Begin("Engine.Rendering.Graph.Compile");
                var graph = ctx.GraphBuilder.Compile(_resourcePool, frame);
                Profiling.End("Engine.Rendering.Graph.Compile");

                Debug.Assert(graph != null,
                    "Frame Graph is empty"); // Since we always prepare for present the graph can never be empty

                // var cmd = frame.GetPrimaryCommandBuffer();
                //
                var cmd = frame.GetPrimaryCommandBuffer();
                cmd
                    .Begin();

                SGraphicsModule.Get().GetImageFactory().Bind(cmd);

                frame.OnReset += _ => graph.Dispose();


                Profiling.Begin("Engine.Rendering.Graph.Execute");
                graph.Execute(frame, ctx, _taskPool);
                Profiling.End("Engine.Rendering.Graph.Execute");

                vkEndCommandBuffer(cmd);

                var queue = _module.GetGraphicsQueue();

                var renderSemaphore = _renderSemaphores[swapchainImageIndex];

                _module.SubmitToQueue(queue, frame.GetRenderFence(), [
                        new VkCommandBufferSubmitInfo
                        {
                            sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                            deviceMask = 0,
                            commandBuffer = frame.GetPrimaryCommandBuffer()
                        }
                    ], [
                        new VkSemaphoreSubmitInfo
                        {
                            sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                            semaphore = renderSemaphore,
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

    private class SwapchainImage : IDeviceImage
    {
        public void Dispose()
        {
        }

        public required ImageFormat Format { get; set; }
        public required Extent3D Extent { get; set; }
        public required VkImage NativeImage { get; set; }
        public required VkImageView NativeView { get; set; }
    }

    private class OutOfDateException : Exception
    {
    }

    private class RenderData : IRenderData
    {
        public required Frame TargetFrame { get; init; }
        public required IGraphBuilder GraphBuilder { get; init; }

        public Extent2D RenderExtent { get; init; }
        public uint SwapchainImageId { get; set; }
        public required IRenderer Renderer { get; init; }
    }
}