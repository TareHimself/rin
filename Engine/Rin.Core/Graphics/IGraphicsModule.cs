// using Rin.Core.Graphics.Meshes;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;
using Rin.Shade;

namespace Rin.Core.Graphics;

public interface  IGraphicsModule : IModule, IUpdatable, IProviderResolvable<IGraphicsModule>
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;

    public IDevice CurrentDevice { get; }
    public IClipboard Clipboard { get; }

    public void AddRenderer(IRenderer renderer);
    public void RemoveRenderer(IRenderer renderer);
    public IWindowRenderer? GetWindowRenderer(IWindow window);
    public IRenderer[] GetRenderers();
    public IWindowRenderer[] GetWindowRenderers();
    public IGraphicsShader MakeGraphics(IGraphicsDescriptor descriptor);
    public IComputeShader MakeCompute(IComputeDescriptor descriptor);

    public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
        IWindow? parent = null);

    public void WaitIdle();

    /// <summary>
    ///     Registers a resource without allocating GPU memory for it - buffer creation itself is CPU-only,
    ///     so unlike texture creation there's no async variant of this.
    /// </summary>
    public ResourceHandle CreateBuffer(ulong size, BufferCreateFlags flags, bool sequentialWrite = true);

    /// <summary>
    ///     Uploads into a buffer via the transfer queue (staging buffer + copy), for buffers not created with
    ///     <see cref="BufferCreateFlags.HostSrc" />/<see cref="BufferCreateFlags.HostDst" />. Mirrors
    ///     <see cref="QueueTextureUpload" />.
    /// </summary>
    public Task QueueBufferUpload(ResourceHandle handle, ReadOnlyMemory<byte> data, ulong offset = 0);

    /// <summary>
    ///     Queues a buffer to buffer copy that runs on the GPU in enqueue order with the other uploads and copies
    ///     of both buffers. The task completes once the GPU has run it and is cancelled if either buffer is freed first.
    /// </summary>
    public Task QueueBufferCopy(ResourceHandle src, ResourceHandle dst, ulong srcOffset, ulong dstOffset, ulong size);

    /// <summary>
    ///     Queues a texture to texture copy, ordered like <see cref="QueueBufferCopy" />. The source needs
    ///     <see cref="ImageCreateFlags.TransferSrc" />.
    /// </summary>
    public Task QueueTextureCopy(ResourceHandle src, ResourceHandle dst, Offset2D srcOffset, Offset2D dstOffset,
        Extent2D extent);

    /// <summary>
    ///     Queues a copy from tightly packed rows at <paramref name="srcOffset" /> in a buffer into a texture region,
    ///     ordered like <see cref="QueueBufferCopy" />.
    /// </summary>
    public Task QueueBufferToTextureCopy(ResourceHandle src, ResourceHandle dst, ulong srcOffset, Offset2D dstOffset,
        Extent2D extent);

    /// <summary>
    ///     Queues a copy of a texture region into tightly packed rows at <paramref name="dstOffset" /> in a buffer,
    ///     ordered like <see cref="QueueBufferCopy" />. The texture needs <see cref="ImageCreateFlags.TransferSrc" />.
    /// </summary>
    public Task QueueTextureToBufferCopy(ResourceHandle src, ResourceHandle dst, Offset2D srcOffset, Extent2D extent,
        ulong dstOffset);

    public ResourceHandle CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageCreateFlags flags = ImageCreateFlags.None);

    public ResourceHandle CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageCreateFlags flags = ImageCreateFlags.None);

    public ResourceHandle CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageCreateFlags flags = ImageCreateFlags.None);

    /// <summary>
    ///     The handle is usable as soon as this returns; its contents arrive the first time a graph uses it or at the
    ///     end of the frame, and until then it samples as the default texture.
    /// </summary>
    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageCreateFlags flags = ImageCreateFlags.None);

    public Task<ResourceHandle> CreateTextureArray(out ResourceHandle handle, ReadOnlySpan<byte> data,
        in Extent2D extent,
        ImageFormat format, uint count, bool mips = false, ImageCreateFlags flags = ImageCreateFlags.None);

    public Task<ResourceHandle> CreateCubemap(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageCreateFlags flags = ImageCreateFlags.None);

    /// <summary>
    ///     Completes once the write has been submitted by the render thread, so never block the update thread on it.
    /// </summary>
    public Task QueueTextureUpload(ResourceHandle handle, ReadOnlyMemory<byte> data, Extent2D extent,
        Offset2D offset = default);

    public bool IsValidResourceHandle(in ResourceHandle handle);

    /// <summary>
    ///     The live handle for the resource currently in <paramref name="device" />'s slot, or an invalid handle when the
    ///     slot is empty or its resource was freed. Recovers the generation a GPU-side <see cref="DeviceHandle" /> drops.
    /// </summary>
    public ResourceHandle ResolveResourceHandle(DeviceHandle device);
    public Extent2D GetExtent(in ResourceHandle handle);
    public ImageFormat GetFormat(in ResourceHandle handle);
    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles);

    /// <summary>
    ///     Sets the allocation's debug name, independent of creation - safe to call any time after the handle
    ///     is valid.
    /// </summary>
    public void SetDebugName(in ResourceHandle handle, string name);

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0);

    public void Collect();
    public void Execute();

    /// <summary>
    ///     The most frames per second <see cref="Execute" /> runs, or 0 for as many as the window allows.
    /// </summary>
    public int MaxFramerate { get; }

    public void SetMaxFramerate(int framesPerSecond);

    public uint GetNumFramesInFlight();

    /// <summary>
    ///     Requests a new frames in flight count. Safe from any thread; the render thread applies it between frames.
    /// </summary>
    public void SetFramesInFlight(uint framesInFlight);

    /// <summary>
    ///     The true cap on how many frames in flight can be requested, imposed by the open windows' presentation
    ///     surfaces.
    /// </summary>
    public uint GetMaxTrueFramesInFlight();
}