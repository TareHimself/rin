using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Graphics.Windows;
using rin.Framework.Graphics.Windows.Events;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;
using NativeGraphicsMethods = rin.Framework.Graphics.NativeMethods;

namespace rin.Framework.Graphics;

/// <summary>
///     Handle's rendering on a <see cref="Windows" />
/// </summary>
public class WindowRenderer : Disposable
{
    private class OutOfDateException : Exception
    {
    }

    private const uint FramesInFlight = 2;
    private readonly SGraphicsModule _module;
    private readonly VkSurfaceKHR _surface;
    private readonly IWindow _window;
    private Frame[] _frames = [];
    private ulong _framesRendered;
    private bool _resizing;
    private bool _resizePending = false;
    private VkSwapchainKHR _swapchain;
    private VkImage[] _swapchainImages = [];
    private readonly HashSet<VkPresentModeKHR> _supportedPresentModes;
    public double LastDrawElapsedTime = 0.0;
    public double LastDrawTime = 0.0;
    private readonly IResourcePool _resourcePool;
    private readonly object _drawLock = new();

    private VkExtent2D _swapchainSize = new()
    {
        width = 0,
        height = 0
    };

    private VkImageView[] _swapchainViews = [];
    private VkViewport _viewport;

    public event Action<Frame>? OnDraw;
    public event Action<Frame, VkImage, VkExtent2D>? OnCopy;

    public event Action<Vec2<uint>>? OnResize;


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

    private unsafe VkSurfaceKHR CreateSurface()
    {
        var instance = SGraphicsModule.Get().GetInstance();
        var surface = new VkSurfaceKHR();
        NativeMethods.CreateSurface(instance, _window.GetPtr(), &surface);
        return surface;
    }

    public uint GetNumFramesInFlight() => FramesInFlight;

    public void Init()
    {
        CreateSwapchain();
        InitFrames();
        _window.OnResized += OnWindowResized;
        _window.OnRefresh += OnWindowRefreshed;
    }


    private void OnWindowResized(ResizeEvent e)
    {
        //CheckSwapchainSize();
        RequestResize();
        DrawFrame();
    }

    private void OnWindowRefreshed(RefreshEvent e)
    {
        RequestResize();
        DrawFrame();
    }

    protected override void OnDispose(bool isManual)
    {
        // _window.OnFocused -= OnFocused;
        // _window.OnMaximized -= OnMaximized;
        lock (_drawLock)
        {
            _module.WaitDeviceIdle();
            _window.OnRefresh -= OnWindowRefreshed;
            _window.OnResized -= OnWindowResized;
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

    private unsafe void CreateSwapchain()
    {
        _viewport.minDepth = 0.0f;
        _viewport.maxDepth = 1.0f;
        var windowSize = _window.GetPixelSize();
        _viewport.width = windowSize.X;
        _viewport.height = windowSize.Y;

        if (_viewport.height == 0 || _viewport.width == 0) return;
        var device = _module.GetDevice();
        var physicalDevice = _module.GetPhysicalDevice();
        var format = _module.GetSurfaceFormat();
        var presentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
        // _supportedPresentModes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR)
        // ? VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR
        // : _supportedPresentModes.First();
        NativeMethods.CreateSwapchain(
            device,
            physicalDevice,
            _surface,
            (int)format.format,
            (int)format.colorSpace,
            (int)presentMode,
            (uint)_viewport.width,
            (uint)_viewport.height,
            (swapchain, swapchainImages, numSwapchainImages, swapchainImageViews, numSwapchainImageViews) =>
            {
                _swapchain = new VkSwapchainKHR(swapchain);
                var images = (VkImage*)swapchainImages;
                var imageViews = (VkImageView*)swapchainImageViews;

                _swapchainImages = new VkImage[numSwapchainImages];

                for (var i = 0; i < numSwapchainImages; i++) _swapchainImages[i] = images[i];

                _swapchainViews = new VkImageView[numSwapchainImageViews];

                for (var i = 0; i < numSwapchainImages; i++) _swapchainViews[i] = imageViews[i];
            });
        _swapchainSize = new VkExtent2D()
        {
            width = windowSize.X,
            height = windowSize.Y
        };
    }

    private void RequestResize()
    {
        _resizePending = true;
    }

    private bool CheckSwapchainSize()
    {
        lock (_drawLock)
        {
            if (_resizing) return true;
            var windowSize = _window.GetPixelSize();
            if (_swapchainSize.width == windowSize.X &&
                _swapchainSize.height == windowSize.Y) return false;

            _resizing = true;
            _module.WaitDeviceIdle();
            DestroySwapchain();
            CreateSwapchain();
            OnResize?.Invoke(new Vec2<uint>(_swapchainSize.width, _swapchainSize.height));
            _resizing = false;
            _resizePending = false;
            return true;
        }
    }


    private unsafe void DestroySwapchain()
    {
        var device = _module.GetDevice();
        foreach (var view in _swapchainViews) vkDestroyImageView(device, view, null);
        _swapchainViews = [];
        _swapchainImages = [];


        if (_swapchain.Value != 0) vkDestroySwapchainKHR(device, _swapchain, null);
        _swapchain = new VkSwapchainKHR();
        _swapchainSize = new VkExtent2D();
    }

    private void InitFrames()
    {
        var frames = new List<Frame>();

        for (var i = 0; i < FramesInFlight; i++) frames.Add(new Frame(this));

        _frames = frames.ToArray();
    }

    private bool ShouldDraw()
    {
        return !_resizing && _swapchainImages.Length > 0;
    }

    public IWindow GetWindow()
    {
        return _window;
    }

    private Frame GetCurrentFrame()
    {
        return _frames[_framesRendered % FramesInFlight];
    }

    public VkExtent3D GetSwapchainExtent()
    {
        return new VkExtent3D
        {
            width = _swapchainSize.width,
            height = _swapchainSize.height,
            depth = 1
        };
    }

    private static void CheckResult(VkResult result)
    {
        switch (result)
        {
            case VkResult.VK_SUCCESS:
                return;
            case VkResult.VK_ERROR_OUT_OF_DATE_KHR:
                throw new OutOfDateException();
            default:
                throw new Exception(result.ToString());
        }
    }

    private void DrawFrame()
    {
        if (_resizePending && !_resizing)
        {
            CheckSwapchainSize();
        }
        
        if (!ShouldDraw()) return;

        lock (_drawLock)
        {
            try
            {
                if (Disposed) return;
                var frame = GetCurrentFrame();
                var device = _module.GetDevice();

                frame.WaitForLastDraw();

                _resourcePool.OnFrameStart(_framesRendered);

                frame.Reset();

                uint swapchainImageIndex = 0;

                unsafe
                {
                    CheckResult(vkAcquireNextImageKHR(device, _swapchain, ulong.MaxValue, frame.GetSwapchainSemaphore(),
                        new VkFence(),
                        &swapchainImageIndex));
                }

                var cmd = frame.GetCommandBuffer();

                vkResetCommandBuffer(cmd, 0);

                var commandBeginInfo = new VkCommandBufferBeginInfo
                {
                    sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
                    flags = VkCommandBufferUsageFlags.VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT
                };

                var swapchainExtent = _swapchainSize;

                unsafe
                {
                    vkBeginCommandBuffer(cmd, &commandBeginInfo);

                    cmd
                        .SetRasterizerDiscard(false)
                        .DisableMultiSampling();
                }

                if ((OnDraw?.GetInvocationList().Length ?? 0) > 0)
                {
                    OnDraw?.Invoke(frame);

                    var graph = frame.GetBuilder().Compile(_resourcePool, frame);
                    graph?.Execute(frame);
                    if (graph != null)
                    {
                        frame.OnReset += (_) => graph.Dispose();
                    }

                    cmd.ImageBarrier(_swapchainImages[swapchainImageIndex],
                        ImageLayout.Undefined,
                        ImageLayout.TransferDst);

                    frame.DoCopy(_swapchainImages[swapchainImageIndex], swapchainExtent);
                    OnCopy?.Invoke(frame, _swapchainImages[swapchainImageIndex], swapchainExtent);

                    cmd.ImageBarrier(_swapchainImages[swapchainImageIndex],
                        ImageLayout.TransferDst,
                        ImageLayout.PresentSrc);
                }
                else
                {
                    cmd.ImageBarrier(_swapchainImages[swapchainImageIndex],
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

                _framesRendered++;
            }
            catch (OutOfDateException e)
            {
                RequestResize(); 
            }
        }
    }

    public void Draw()
    {
        if (Disposed) return;
        var start = SRuntime.Get().GetTimeSeconds();
        DrawFrame();
        LastDrawElapsedTime = SRuntime.Get().GetTimeSeconds() - start;
    }
}