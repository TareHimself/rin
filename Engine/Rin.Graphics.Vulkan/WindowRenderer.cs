using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Graph;
using Rin.Graphics.Vulkan.Images;
using Rin.Graphics.Vulkan.Windows;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The renderer behind a window. It owns the surface and swapchain, acquires an image in <see cref="BeginFrame" />
///     and registers it as a terminal external image of the module's graph, moves it to the present layout in
///     <see cref="EndFrame" /> and describes the present the module records through <see cref="PendingPresent" />.
/// </summary>
public class WindowRenderer : IWindowRenderer
{
    private readonly CoalescingValue<uint> _framesInFlight = new(2);
    private readonly VulkanGraphicsModule _module;
    private readonly VkPresentModeKHR _nonVSyncPresentMode;
    private readonly List<RetiredSwapchain> _retiring = [];
    private readonly VkSurfaceKHR _surface;
    private readonly VkPresentModeKHR _vsyncPresentMode;
    private readonly IWindow _window;
    private SwapchainImage? _acquired;
    private VkSemaphore _acquiredWith;
    private VkSemaphore[] _acquireSemaphores = [];
    private bool _dirty;
    private bool _disposed;
    private VkSemaphore[] _renderSemaphores = [];
    private VkSwapchainKHR _swapchain;
    private Extent2D _swapchainExtent;
    private VkImage[] _swapchainImages = [];
    private SwapchainImage[] _swapchainImageWrappers = [];
    private VkImageView[] _swapchainViews = [];

    public WindowRenderer(VulkanGraphicsModule module, IWindow window, in VkSurfaceKHR? surface = null)
    {
        _module = module;
        _window = window;
        _surface = surface ?? CreateSurface();
        var presentModes = module.GetPhysicalDevice().GetSurfacePresentModes(_surface).ToHashSet();

        if (presentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR;
        else if (presentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_FIFO_RELAXED_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_RELAXED_KHR;
        else if (presentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR))
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR;
        else
            _nonVSyncPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;

        _vsyncPresentMode = presentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR)
            ? VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR
            : VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
    }

    private VkPresentModeKHR PresentMode => VsyncEnabled ? _vsyncPresentMode : _nonVSyncPresentMode;

    /// <summary>
    ///     The swapchain, image and semaphores of the image acquired by the last successful <see cref="BeginFrame" />.
    ///     Valid until <see cref="CompletePresent" />.
    /// </summary>
    internal PresentRequest PendingPresent
    {
        get
        {
            var image = _acquired ?? throw new InvalidOperationException("No image has been acquired for this frame");
            return new PresentRequest(_swapchain, image.Index, _acquiredWith, _renderSemaphores[image.Index]);
        }
    }

    public IWindow GetWindow()
    {
        return _window;
    }

    public Extent2D GetRenderExtent()
    {
        var capabilities = GetSurfaceCapabilities();
        return new Extent2D(capabilities.currentExtent.width, capabilities.currentExtent.height);
    }

    public bool VsyncEnabled { get; set; }

    public void SetVsyncEnabled(bool enabled)
    {
        if (VsyncEnabled == enabled) return;
        VsyncEnabled = enabled;
        MarkDirty();
    }

    public void Dispose()
    {
        _disposed = true;
        _module.WaitIdle();
        RetireSwapchain()?.Dispose();
        foreach (var retired in _retiring) retired.Dispose();
        _retiring.Clear();
        var device = _module.GetDevice();
        foreach (var semaphore in _acquireSemaphores) device.DestroySemaphore(semaphore);
        _acquireSemaphores = [];
        unsafe
        {
            vkDestroySurfaceKHR(_module.GetInstance(), _surface, null);
        }
    }

    public IRenderData? Collect()
    {
        if (_disposed || OnCollect == null) return null;

        var collector = new GraphCollector();
        OnCollect.Invoke(collector);

        return new RenderData
        {
            Renderer = this,
            Collector = collector,
            RenderExtent = _window.GetSize()
        };
    }

    public bool BeginFrame(IRenderData data, IGraphBuilder builder)
    {
        if (_disposed || data is not RenderData render) return false;
        if (render.RenderExtent.Width == 0 || render.RenderExtent.Height == 0) return false;
        if (render.RenderExtent != _window.GetSize()) return false;

        ApplyPendingFramesInFlight();
        if (_swapchainExtent != render.RenderExtent) MarkDirty();

        var frame = _module.CurrentFrame;
        if (!RebuildSwapchainIfDirty(frame, render.RenderExtent)) return false;
        if (!TryAcquireImage(frame, out var image)) return false;

        // Contents and layout are undefined after a present, so the graph's first barrier starts from Undefined.
        image.Layout = ImageLayout.Undefined;
        DestinationImageId = builder.AddExternalImage(image.Handle, GraphResourceFlags.Terminal);
        _acquired = image;
        render.Collector.Write(builder);
        return true;
    }

    public void EndFrame(IExecutionContext ctx)
    {
        var image = _acquired ?? throw new InvalidOperationException("EndFrame without a successful BeginFrame");
        ((VulkanExecutionContext)ctx).CommandBuffer.ImageBarrier(image, image.Layout, ImageLayout.Present);
    }

    /// <summary>
    ///     Takes this window's result of the shared present. Out of date and suboptimal rebuild the swapchain next frame.
    /// </summary>
    internal void CompletePresent(VkResult result)
    {
        _acquired = null;
        switch (result)
        {
            case VkResult.VK_ERROR_OUT_OF_DATE_KHR:
            case VkResult.VK_SUBOPTIMAL_KHR:
                MarkDirty();
                return;
            default:
                result.ThrowIfFailed(nameof(vkQueuePresentKHR));
                return;
        }
    }

    public event Action<IGraphCollector>? OnCollect;

    /// <summary>
    ///     Marks this renderer as dirty and will cause the swapchain to be re-created next frame
    /// </summary>
    private void MarkDirty()
    {
        _dirty = true;
    }

    private VkSurfaceKHR CreateSurface()
    {
        var instance = _module.GetInstance();
        var rinWindow = _window as RinWindow ?? throw new NullReferenceException();
        return Native.CreateSurface(instance, rinWindow.GetHandle());
    }

    public uint DestinationImageId { get; private set; }

    public uint GetNumFramesInFlight()
    {
        return _framesInFlight.Current;
    }

    /// <summary>
    ///     Publishes a new swapchain image count request. Safe to call from any thread at any time - it never touches
    ///     GPU resources itself. The render loop picks up the latest request and rebuilds the swapchain at its next safe
    ///     point (<see cref="BeginFrame" />). The frame ring itself belongs to the module.
    /// </summary>
    public void SetFramesInFlight(uint framesInFlight)
    {
        _framesInFlight.Set(Math.Max(1, framesInFlight));
    }

    private void ApplyPendingFramesInFlight()
    {
        if (_framesInFlight.TryConsume(out _)) MarkDirty();
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

    /// <summary>
    ///     Creates one acquire semaphore per slot of the module's frame ring. Render semaphores belong to the swapchain
    ///     and are created with it.
    /// </summary>
    public void Init()
    {
        var device = _module.GetDevice();
        _acquireSemaphores = new VkSemaphore[_module.FramesInFlight];
        for (var i = 0; i < _acquireSemaphores.Length; i++) _acquireSemaphores[i] = device.CreateSemaphore();
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
                Index = (uint)i,
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
    ///     after every frame that could reference it has finished, so nothing waits on the device here. Disposing the
    ///     renderer first destroys it earlier, because a swapchain has to go before its surface.
    /// </summary>
    private bool RebuildSwapchainIfDirty(Frame frame, Extent2D extent)
    {
        if (!_dirty) return true;

        var retired = RetireSwapchain();
        if (retired != null)
        {
            _retiring.Add(retired);
            frame.OnReset += _ => DisposeRetired(retired);
        }

        if (!CreateSwapchain(extent, retired?.Swapchain ?? default)) return false;
        _dirty = false;
        return true;
    }

    private void DisposeRetired(RetiredSwapchain retired)
    {
        if (_retiring.Remove(retired)) retired.Dispose();
    }

    private unsafe bool TryAcquireImage(Frame frame, out SwapchainImage image)
    {
        var semaphore = _acquireSemaphores[frame.Index];
        uint index = 0;
        var result = vkAcquireNextImageKHR(_module.GetDevice(), _swapchain, ulong.MaxValue, semaphore, new VkFence(),
            &index);
        switch (result)
        {
            case VkResult.VK_ERROR_OUT_OF_DATE_KHR:
                MarkDirty();
                image = null!;
                return false;
            // Suboptimal still hands out an image and signals the semaphore, so the frame must consume it.
            case VkResult.VK_SUBOPTIMAL_KHR:
                MarkDirty();
                break;
            default:
                result.ThrowIfFailed(nameof(vkAcquireNextImageKHR));
                break;
        }

        _acquiredWith = semaphore;
        image = _swapchainImageWrappers[index];
        return true;
    }

    /// <summary>
    ///     What the module waits on, signals and presents for one window in the frame's shared submit and present.
    /// </summary>
    internal readonly record struct PresentRequest(
        VkSwapchainKHR Swapchain,
        uint ImageIndex,
        VkSemaphore AcquireSemaphore,
        VkSemaphore RenderSemaphore);

    private class SwapchainImage : IDisposableVulkanTexture
    {
        public uint Index { get; init; }
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
        public required GraphCollector Collector { get; init; }
        public Extent2D RenderExtent { get; init; }
        public required IRenderer Renderer { get; init; }
    }
}
