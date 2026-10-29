using System.Collections.Frozen;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Graphics.Vulkan.Graph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The frame ring and the one graph per frame: every registered renderer adds its passes to the same builder, the
///     compiled graph records into the frame's command buffer, and one submit and one present carry every window.
/// </summary>
public partial class VulkanGraphicsModule
{
    private const uint DefaultFramesInFlight = 2;

    private readonly List<IRenderer> _active = [];
    private readonly List<IRenderData> _batch = [];
    private readonly Lock _frameLock = new();
    private readonly CoalescingValue<uint> _framesInFlight = new(DefaultFramesInFlight);
    private readonly List<WindowRenderer> _presenting = [];
    private readonly ResourcePool _resourcePool = new();
    private Frame[] _frameRing = [];
    private ulong _framesRendered;

    internal IResourcePool ResourcePool => _resourcePool;

    internal FrozenDictionary<uint, DescriptorSet> GlobalDescriptors { get; private set; } =
        FrozenDictionary<uint, DescriptorSet>.Empty;

    /// <summary>
    ///     The frame being recorded. Valid on the render thread from the frame's reset until its present.
    /// </summary>
    internal Frame CurrentFrame { get; private set; } = null!;

    internal uint FramesInFlight => (uint)_frameRing.Length;

    public uint GetNumFramesInFlight()
    {
        return _framesInFlight.Current;
    }

    public void SetFramesInFlight(uint framesInFlight)
    {
        _framesInFlight.Set(Math.Max(1, framesInFlight));
    }

    public uint GetMaxTrueFramesInFlight()
    {
        var max = uint.MaxValue;
        foreach (var renderer in _frames.GetWindowRenderers())
            if (renderer is WindowRenderer window)
                max = Math.Min(max, window.MaxSwapchainImages);
        return max;
    }

    /// <summary>
    ///     Applies the latest frames in flight request between frames: waits for the device, rebuilds the ring and
    ///     tells every window to follow with new acquire semaphores and a swapchain of the matching image count.
    /// </summary>
    private void ApplyPendingFramesInFlight()
    {
        if (!_framesInFlight.TryConsume(out var framesInFlight)) return;

        WaitIdle();
        DisposeFrames();
        InitFrames(framesInFlight);
        foreach (var renderer in _frames.GetWindowRenderers())
            if (renderer is WindowRenderer window)
                window.OnFramesInFlightChanged();
    }

    private void InitFrames(uint framesInFlight)
    {
        DescriptorSet[] sets = [GetResourceDescriptorSet()];
        GlobalDescriptors = sets.ToFrozenDictionary(_ => 0u, set => set);
        _frameRing = new Frame[framesInFlight];
        for (var i = 0u; i < framesInFlight; i++) _frameRing[i] = new Frame(this, i);
    }

    private void DisposeFrames()
    {
        foreach (var frame in _frameRing) frame.Dispose();
        _frameRing = [];
    }

    /// <summary>
    ///     Renders the newest collected batch, if there is one. Holding <see cref="_frameLock" /> from the first
    ///     <see cref="IRenderer.BeginFrame" /> to the present keeps renderer removal out of a frame in progress.
    /// </summary>
    private void RenderCollectedFrame()
    {
        lock (_frameLock)
        {
            if (!_frames.TryTake(_batch)) return;
            ApplyPendingFramesInFlight();
            if (_batch.Count > 0) RenderBatch();
            FrameCollector.DisposeBatch(_batch);
            _batch.Clear();
        }
    }

    private unsafe void RenderBatch()
    {
        var frame = _frameRing[(int)(_framesRendered % (ulong)_frameRing.Length)];
        CurrentFrame = frame;
        frame.WaitForLastDraw();
        frame.Reset();
        _resourcePool.OnFrameEnd(_framesRendered);

        var builder = frame.GraphBuilder;
        builder.Reset();
        _active.Clear();
        _presenting.Clear();

        Profiling.Begin("Engine.Rendering.Graph.Build");
        foreach (var data in _batch)
            if (data.Renderer.BeginFrame(data, builder))
                _active.Add(data.Renderer);
        Profiling.End("Engine.Rendering.Graph.Build");

        Profiling.Begin("Engine.Rendering.Graph.Compile");
        var graph = builder.Compile();
        Profiling.End("Engine.Rendering.Graph.Compile");

        var cmd = frame.GetPrimaryCommandBuffer();
        cmd.Begin();
        BindBindlessDescriptors(cmd);

        if (graph != null)
        {
            frame.OnReset += _ => graph.Dispose();
            Profiling.Begin("Engine.Rendering.Graph.Execute");
            graph.Execute(frame.ExecutionContext);
            Profiling.End("Engine.Rendering.Graph.Execute");
        }

        foreach (var renderer in _active)
        {
            renderer.EndFrame(frame.ExecutionContext);
            if (renderer is WindowRenderer window) _presenting.Add(window);
        }

        vkEndCommandBuffer(cmd).ThrowIfFailed(nameof(vkEndCommandBuffer));

        Submit(frame);
        (graph as CompiledGraph)?.OnSubmitted();

        // The submit owns the fence either way, so the frame is in flight before any present result decides whether
        // a swapchain has to be rebuilt.
        frame.Finish();
        _framesRendered++;

        Present();
    }

    /// <summary>
    ///     One submit of the frame's command buffer that waits every presenting window's acquire semaphore and signals
    ///     its render semaphore.
    /// </summary>
    private unsafe void Submit(Frame frame)
    {
        var count = _presenting.Count;
        var waits = stackalloc VkSemaphoreSubmitInfo[count];
        var signals = stackalloc VkSemaphoreSubmitInfo[count];
        for (var i = 0; i < count; i++)
        {
            var present = _presenting[i].PendingPresent;
            waits[i] = new VkSemaphoreSubmitInfo
            {
                sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                semaphore = present.AcquireSemaphore,
                value = 1,
                stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
            };
            signals[i] = new VkSemaphoreSubmitInfo
            {
                sType = VK_STRUCTURE_TYPE_SEMAPHORE_SUBMIT_INFO,
                semaphore = present.RenderSemaphore,
                value = 1,
                stageMask = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
            };
        }

        var commandBuffer = new VkCommandBufferSubmitInfo
        {
            sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_SUBMIT_INFO,
            deviceMask = 0,
            commandBuffer = frame.GetPrimaryCommandBuffer()
        };

        SubmitToQueue(_graphicsQueue, frame.GetRenderFence(),
            new ReadOnlySpan<VkCommandBufferSubmitInfo>(&commandBuffer, 1),
            new ReadOnlySpan<VkSemaphoreSubmitInfo>(signals, count),
            new ReadOnlySpan<VkSemaphoreSubmitInfo>(waits, count));
    }

    /// <summary>
    ///     One present for every window that drew this frame. Each swapchain gets its own result, so one out of date
    ///     window does not hide the others'.
    /// </summary>
    private unsafe void Present()
    {
        var count = _presenting.Count;
        if (count == 0) return;

        var swapchains = stackalloc VkSwapchainKHR[count];
        var imageIndices = stackalloc uint[count];
        var renderSemaphores = stackalloc VkSemaphore[count];
        var results = stackalloc VkResult[count];
        for (var i = 0; i < count; i++)
        {
            var present = _presenting[i].PendingPresent;
            swapchains[i] = present.Swapchain;
            imageIndices[i] = present.ImageIndex;
            renderSemaphores[i] = present.RenderSemaphore;
        }

        var presentInfo = new VkPresentInfoKHR
        {
            sType = VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
            waitSemaphoreCount = (uint)count,
            pWaitSemaphores = renderSemaphores,
            swapchainCount = (uint)count,
            pSwapchains = swapchains,
            pImageIndices = imageIndices,
            pResults = results
        };

        var result = vkQueuePresentKHR(_graphicsQueue, &presentInfo);
        if (result is not (VkResult.VK_ERROR_OUT_OF_DATE_KHR or VkResult.VK_SUBOPTIMAL_KHR))
            result.ThrowIfFailed(nameof(vkQueuePresentKHR));

        for (var i = 0; i < count; i++) _presenting[i].CompletePresent(results[i]);
    }
}
