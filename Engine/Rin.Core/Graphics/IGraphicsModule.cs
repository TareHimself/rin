// using Rin.Core.Graphics.Meshes;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics;

public interface  IGraphicsModule : IModule, IUpdatable, IProviderResolvable<IGraphicsModule>
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;

    public IDevice CurrentDevice { get; }

    public void AddRenderer(IRenderer renderer);
    public void RemoveRenderer(IRenderer renderer);
    public IWindowRenderer? GetWindowRenderer(IWindow window);
    public IRenderer[] GetRenderers();
    public IWindowRenderer[] GetWindowRenderers();
    public IGraphicsShader MakeGraphics(string path);
    public IComputeShader MakeCompute(string path);

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
    public Extent2D GetExtent(in ResourceHandle handle);
    public ImageFormat GetFormat(in ResourceHandle handle);
    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles);

    /// <summary>
    ///     Sets the allocation's debug name, independent of creation - safe to call any time after the handle
    ///     is valid.
    /// </summary>
    public void SetDebugName(in ResourceHandle handle, string name);

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0);
    public ulong GetBufferAddress(in ResourceHandle handle);

    public void Collect();
    public void Execute();
}