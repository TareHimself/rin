using System.Collections.Frozen;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Graphics.Vulkan.Graph;
using Rin.Graphics.Vulkan.Images;
using Rin.Graphics.Vulkan.Windows;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     Handle's rendering on a <see cref="Windows" />
/// </summary>
public class WindowRenderer : IWindowRenderer
{
    private readonly CoalescingValue<uint> _framesInFlight = new(2);

    private readonly Lock _drawLock = new();
    private readonly VulkanGraphicsModule _module;
    private readonly VkPresentModeKHR _nonVSyncPresentMode;
    public readonly IResourcePool ResourcePool;
    private readonly VkSurfaceKHR _surface;
    private readonly VkPresentModeKHR _vsyncPresentMode;
    private readonly IWindow _window;
    private bool _dirty;
    private bool _disposed;
    private Frame[] _frames = [];
    private ulong _framesCollected;
    private ulong _framesRendered;
    public FrozenDictionary<uint, DescriptorSet> GlobalDescriptors = FrozenDictionary<uint, DescriptorSet>.Empty;
    private VkSemaphore[] _renderSemaphores = [];
    private VkSwapchainKHR _swapchain;

    private Extent2D _swapchainExtent = new()
    {
        Width = 0,
        Height = 0
    };

    private VkImage[] _swapchainImages = [];

    private VkImageView[] _swapchainViews = [];

    // One handle-registered wrapper per swapchain image, created once alongside the swapchain and reused every
    // frame -- avoids allocating a new BindlessTexture + ResourceHandle registration on every single draw.
    private SwapchainImage[] _swapchainImageWrappers = [];

    public WindowRenderer(VulkanGraphicsModule module, IWindow window, in VkSurfaceKHR? surface = null)
    {
        _module = module;
        _window = window;
        _surface = surface ?? CreateSurface();
        var supportedPresentModes = module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();
        ResourcePool = new ResourcePool();
        SetupGlobalDescriptors();
        var presentModesSet = supportedPresentModes.ToHashSet();

        if (presentModesSet.Contains(VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR;
        else if (presentModesSet.Contains(VkPresentModeKHR.VK_PRESENT_MODE_FIFO_RELAXED_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_RELAXED_KHR;
        else if (presentModesSet.Contains(VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR;
        else
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;

        _vsyncPresentMode = presentModesSet.Contains(VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR) ? VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR : VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
    }

    private VkPresentModeKHR PresentMode => VsyncEnabled ? _vsyncPresentMode : _nonVSyncPresentMode;

    public IWindow GetWindow()
    {
        return _window;
    }

    public Extent2D GetRenderExtent()
    {
        unsafe
        {
            var surfaceCapabilities = new VkSurfaceCapabilitiesKHR();
            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_module.GetPhysicalDevice(), _surface, &surfaceCapabilities)
                .ThrowIfFailed(nameof(vkGetPhysicalDeviceSurfaceCapabilitiesKHR));
            return new Extent2D
            {
                Width = surfaceCapabilities.currentExtent.width,
                Height = surfaceCapabilities.currentExtent.height
            };
        }
    }

    public bool VsyncEnabled { get; set; }

    public void SetVsyncEnabled(bool enabled)
    {
        if (VsyncEnabled != enabled)
        {
            VsyncEnabled = enabled;
            MarkDirty();
        }
    }

    public void Dispose()
    {
        lock (_drawLock)
        {
            _disposed = true;
            _module.WaitIdle();
            foreach (var frame in _frames) frame.Dispose();
            ResourcePool.Dispose();
            _frames = [];
            RetireSwapchain()?.Dispose();
            unsafe
            {
                vkDestroySurfaceKHR(_module.GetInstance(), _surface, null);
            }
        }
    }

    public IRenderData? Collect()
    {
        return DoCollect();
    }

    public void Execute(IRenderData context)
    {
        if (_disposed) return;

        if (context is not RenderData ctx) return;

        if (ctx.RenderExtent.Width == 0 || ctx.RenderExtent.Height == 0) return;

        if (ctx.RenderExtent != _window.GetSize()) return;

        ApplyPendingFramesInFlight();

        if (_swapchainExtent != ctx.RenderExtent) MarkDirty();

        DoExecute(ctx);
    }

    public event Action<IGraphCollector>? OnCollect;


    /// <summary>
    ///     Marks this renderer as dirty and will cause the swapchain to be re-created next frame
    /// </summary>
    private void MarkDirty()
    {
        _dirty = true;
    }

    private void SetupGlobalDescriptors()
    {
        var globalDescriptor = _module.GetResourceDescriptorSet();
        DescriptorSet[] sets = [globalDescriptor];
        GlobalDescriptors = sets.ToFrozenDictionary(_ => (uint)0, d => d);
    }

    private VkSurfaceKHR CreateSurface()
    {
        var instance = _module.GetInstance();
        var rinWindow = _window as RinWindow ?? throw new NullReferenceException();
        // ReSharper disable once AccessToDisposedClosure
        return Native.CreateSurface(instance, rinWindow.GetHandle());
    }

    public uint DestinationImageId { get; private set; }

    public uint GetNumFramesInFlight()
    {
        return _framesInFlight.Current;
    }

    /// <summary>
    ///     Publishes a new frames-in-flight request. Safe to call from any thread at any time - it
    ///     never touches GPU resources itself. The render loop picks up the latest request and
    ///     recreates frame resources and the swapchain at its next safe point (<see cref="Execute" />).
    /// </summary>
    public void SetFramesInFlight(uint framesInFlight)
    {
        _framesInFlight.Set(Math.Max(1, framesInFlight));
    }

    /// <summary>
    ///     Applies the latest published frames-in-flight request, if any, by recreating frame
    ///     resources and marking the swapchain dirty. Only safe to call from the render loop, between
    ///     frames.
    /// </summary>
    private void ApplyPendingFramesInFlight()
    {
        if (!_framesInFlight.TryConsume(out _)) return;

        if (_frames.Length > 0)
        {
            _module.WaitIdle();
            foreach (var frame in _frames) frame.Dispose();
            InitFrames();
        }

        MarkDirty();
    }

    public unsafe uint GetMaxTrueFramesInFlight()
    {
        var capabilities = GetSurfaceCapabilities();
        // 0 means the surface imposes no upper bound (Vulkan spec).
        return capabilities.maxImageCount == 0 ? uint.MaxValue : capabilities.maxImageCount;
    }

    private unsafe VkSurfaceCapabilitiesKHR GetSurfaceCapabilities()
    {
        var surfaceCapabilities = new VkSurfaceCapabilitiesKHR();
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_module.GetPhysicalDevice(), _surface, &surfaceCapabilities)
            .ThrowIfFailed(nameof(vkGetPhysicalDeviceSurfaceCapabilitiesKHR));
        return surfaceCapabilities;
    }

    public void Init()
    {
        InitFrames();
    }

    private unsafe bool CreateSwapchain(Extent2D extent, VkSwapchainKHR oldSwapchain)
    {
        var surfaceCapabilities = GetSurfaceCapabilities();

        if (surfaceCapabilities.minImageExtent.width > extent.Width ||
            surfaceCapabilities.minImageExtent.height > extent.Height ||
            surfaceCapabilities.maxImageExtent.width < extent.Width ||
            surfaceCapabilities.maxImageExtent.height < extent.Height)
            return false;

        var device = _module.GetDevice();
        var format = _module.GetSurfaceFormat();

        // Use frames-in-flight exactly when the surface allows it, otherwise clamp into its valid
        // [minImageCount, maxImageCount] range (maxImageCount == 0 means no upper bound).
        var minImages = Math.Max(1, surfaceCapabilities.minImageCount);
        var maxImages = surfaceCapabilities.maxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.maxImageCount;
        var imageCount = Math.Clamp(Math.Max(1, _framesInFlight.Current), minImages, maxImages);

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = _surface,
            presentMode = PresentMode,
            imageFormat = format.format,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            imageColorSpace = format.colorSpace,
            imageExtent = extent.ToVk(),
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                         VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            imageArrayLayers = 1,
            minImageCount = imageCount,
            preTransform = VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR,
            oldSwapchain = oldSwapchain
        };

        var swapchain = new VkSwapchainKHR();
        vkCreateSwapchainKHR(device, &createInfo, null, &swapchain).ThrowIfFailed(nameof(vkCreateSwapchainKHR));

        uint imagesCount = 0;
        vkGetSwapchainImagesKHR(device, swapchain, &imagesCount, null).ThrowIfFailed(nameof(vkGetSwapchainImagesKHR));

        _swapchainImages = new VkImage[(int)imagesCount];
        _renderSemaphores = Enumerable.Range(0, (int)imagesCount).Select(_ => device.CreateSemaphore()).ToArray();

        fixed (VkImage* imagesPtr = _swapchainImages)
        {
            vkGetSwapchainImagesKHR(device, swapchain, &imagesCount, imagesPtr)
                .ThrowIfFailed(nameof(vkGetSwapchainImagesKHR));
        }

        _swapchainViews = _swapchainImages.Select(c =>
        {
            var viewCreateInfo = VulkanGraphicsModule.MakeImageViewCreateInfo(ImageFormat.Swapchain, c,
                VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            var view = new VkImageView();
            vkCreateImageView(device, &viewCreateInfo, null, &view).ThrowIfFailed(nameof(vkCreateImageView));
            return view;
        }).ToArray();
        _swapchain = swapchain;
        _swapchainExtent = extent;

        _swapchainImageWrappers = new SwapchainImage[_swapchainImages.Length];
        for (var i = 0; i < _swapchainImages.Length; i++)
        {
            var wrapper = new SwapchainImage
            {
                Format = ImageFormat.Swapchain,
                Extent = extent,
                VulkanImage = _swapchainImages[i],
                VulkanView = _swapchainViews[i]
            };
            wrapper.Handle = _module.RegisterExternalTexture(wrapper);
            _swapchainImageWrappers[i] = wrapper;
        }

        return true;
    }

    /// <summary>
    ///     Takes the current swapchain out of use. Frames already submitted may still reference it, so the caller
    ///     disposes the result once those frames are done.
    /// </summary>
    private RetiredSwapchain? RetireSwapchain()
    {
        if (_swapchain.Value == 0) return null;

        var retired = new RetiredSwapchain(_module, _swapchain, _swapchainViews, _renderSemaphores,
            _swapchainImageWrappers);
        _swapchain = new VkSwapchainKHR();
        _swapchainExtent = new Extent2D();
        _swapchainImages = [];
        _swapchainViews = [];
        _renderSemaphores = [];
        _swapchainImageWrappers = [];
        return retired;
    }

    /// <summary>
    ///     Rebuilds a dirty swapchain. The old one is destroyed when <paramref name="frame" /> is next reset, which is
    ///     after every frame that could reference it has finished, so nothing waits on the device here.
    /// </summary>
    private bool RebuildSwapchainIfDirty(Frame frame, Extent2D extent)
    {
        if (!_dirty) return true;

        var retired = RetireSwapchain();
        if (retired != null) frame.OnReset += _ => retired.Dispose();
        if (!CreateSwapchain(extent, retired?.Swapchain ?? default)) return false;
        _dirty = false;
        return true;
    }

    private void InitFrames()
    {
        var framesInFlight = _framesInFlight.Current;
        _frames = new Frame[framesInFlight];
        for (var i = 0; i < framesInFlight; i++)
        {
            _frames[i] = new Frame(this);
        }
    }

    private static void CheckSwapchainResult(VkResult result, string call)
    {
        switch (result)
        {
            case VkResult.VK_ERROR_OUT_OF_DATE_KHR:
            case VkResult.VK_SUBOPTIMAL_KHR:
                throw new OutOfDateException();
            default:
                result.ThrowIfFailed(call);
                return;
        }
    }

    public IRenderData? DoCollect()
    {
        if (_disposed) return null;
        if (OnCollect == null) return null;

        var frame = _frames[_framesCollected % _framesInFlight.Current];
        _framesCollected++;

        var extent = _window.GetSize();

        var collector = new GraphCollector();

        OnCollect?.Invoke(collector);

        return new RenderData
        {
            Renderer = this,
            TargetFrame = frame,
            Collector = collector,
            RenderExtent = extent
        };
    }

    
    private void DoExecute(RenderData ctx)
    {
        lock (_drawLock)
        {
            try
            {
                unsafe
                {
                    var frame = ctx.TargetFrame;
                    var builder = frame.GraphBuilder;
                    builder.Reset();
                    Profiling.Begin("Engine.Rendering.Graph.Build");
                    ctx.Collector.Write(builder); // Build the passes from the collector
                    Profiling.End("Engine.Rendering.Graph.Build");
                    var device = _module.GetDevice();
                    frame.WaitForLastDraw();

                    frame.Reset();
                    ResourcePool.OnFrameEnd(_framesRendered);

                    if (!RebuildSwapchainIfDirty(frame, ctx.RenderExtent)) return;

                    uint swapchainImageIndex = 0;
                    var acquireResult = vkAcquireNextImageKHR(device, _swapchain, ulong.MaxValue,
                        frame.GetSwapchainSemaphore(), new VkFence(), &swapchainImageIndex);
                    // Suboptimal still hands out an image and signals the semaphore, so the frame must consume it.
                    if (acquireResult == VkResult.VK_SUBOPTIMAL_KHR) MarkDirty();
                    else CheckSwapchainResult(acquireResult, nameof(vkAcquireNextImageKHR));

                    var swapchainImage = _swapchainImageWrappers[swapchainImageIndex];
                    swapchainImage.Layout = ImageLayout.Undefined;
                    DestinationImageId = builder.AddExternalImage(swapchainImage.Handle, GraphResourceFlags.Terminal);

                    Profiling.Begin("Engine.Rendering.Graph.Compile");
                    var graph = builder.Compile();
                    Profiling.End("Engine.Rendering.Graph.Compile");

                    var cmd = frame.GetPrimaryCommandBuffer();
                    cmd
                        .Begin();

                    _module.BindBindlessDescriptors(cmd);

                    if (graph != null)
                    {
                        frame.OnReset += _ => graph.Dispose();
                        Profiling.Begin("Engine.Rendering.Graph.Execute");
                        graph.Execute(frame.ExecutionContext);
                        Profiling.End("Engine.Rendering.Graph.Execute");
                    }

                    cmd.ImageBarrier(swapchainImage, swapchainImage.Layout, ImageLayout.Present);
                    vkEndCommandBuffer(cmd).ThrowIfFailed(nameof(vkEndCommandBuffer));

                    var queue = _module.GetGraphicsQueue();

                    var renderSemaphore = _renderSemaphores[swapchainImageIndex];

                    var bufferSubmitInfo = new VkCommandBufferSubmitInfo
                    {
                        sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
                        deviceMask = 0,
                        commandBuffer = frame.GetPrimaryCommandBuffer()
                    };
                    var signalSemaphoreSubmitInfo = new VkSemaphoreSubmitInfo
                    {
                        sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                        semaphore = renderSemaphore,
                        value = 1,
                        stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
                    };
                    var waitSemaphoreSubmitInfo = new VkSemaphoreSubmitInfo
                    {
                        sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                        semaphore = frame.GetSwapchainSemaphore(),
                        value = 1,
                        stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
                    };

                    _module.SubmitToQueue(queue, frame.GetRenderFence(),
                        new ReadOnlySpan<VkCommandBufferSubmitInfo>(&bufferSubmitInfo, 1),
                        new ReadOnlySpan<VkSemaphoreSubmitInfo>(&signalSemaphoreSubmitInfo, 1),
                        new ReadOnlySpan<VkSemaphoreSubmitInfo>(&waitSemaphoreSubmitInfo, 1));
                    (graph as CompiledGraph)?.OnSubmitted();

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

                    var presentResult = vkQueuePresentKHR(queue, &presentInfo);

                    // The submit above owns the fence either way, so the frame is in flight before the result
                    // decides whether the swapchain has to be rebuilt.
                    frame.Finish();
                    _framesRendered++;

                    CheckSwapchainResult(presentResult, nameof(vkQueuePresentKHR));
                }
            }
            catch (OutOfDateException)
            {
                MarkDirty();
            }
        }
    }

    private class SwapchainImage : IDisposableVulkanTexture
    {
        public VkImage VulkanImage { get; set; }
        public VkImageView VulkanView { get; set; }
        public ImageLayout Layout { get; set; }
        public IntPtr Allocation { get; } = IntPtr.Zero;
        public Extent2D Extent { get; set; }
        public bool Mips { get; } = false;
        public ImageFormat Format { get; set; }
        public ResourceHandle Handle { get; set; } = ResourceHandle.InvalidTexture;

        // Owned by the swapchain, not us -- nothing to release here.
        public void Dispose()
        {
        }
    }

    private class OutOfDateException : Exception
    {
    }

    private sealed class RetiredSwapchain(
        VulkanGraphicsModule module,
        VkSwapchainKHR swapchain,
        VkImageView[] views,
        VkSemaphore[] renderSemaphores,
        SwapchainImage[] wrappers) : IDisposable
    {
        public VkSwapchainKHR Swapchain => swapchain;

        public unsafe void Dispose()
        {
            var device = module.GetDevice();
            foreach (var semaphore in renderSemaphores) device.DestroySemaphore(semaphore);
            foreach (var view in views) vkDestroyImageView(device, view, null);
            foreach (var wrapper in wrappers) module.FreeResourceHandles(wrapper.Handle);
            vkDestroySwapchainKHR(device, swapchain, null);
        }
    }

    private class RenderData : IRenderData
    {
        public required Frame TargetFrame { get; init; }
        public required GraphCollector Collector { get; init; }
        public Extent2D RenderExtent { get; init; }
        public required IRenderer Renderer { get; init; }
    }
}
