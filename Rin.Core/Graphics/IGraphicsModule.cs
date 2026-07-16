using Rin.Core.Graphics.Meshes;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics;

public interface IGraphicsModule : IModule, IUpdatable
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;


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

    public DeviceBufferView NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer");

    public DeviceBufferView NewStorageBuffer<T>(bool sequentialWrite = true)
        where T : unmanaged
    {
        return NewStorageBuffer(Utils.ByteSizeOf<T>(), sequentialWrite);
    }

    public DeviceBufferView NewStorageBuffer(ulong size, bool sequentialWrite = true);
    public DeviceBufferView NewUniformBuffer(ulong size, bool sequentialWrite = true);

    public ResourceHandle CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);

    public ResourceHandle CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);

    public ResourceHandle CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);

    public Task CreateTexture(out ResourceHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None);

    public Task CreateTextureArray(out ResourceHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None);

    public Task CreateCubemap(out ResourceHandle handle, IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None);

    public bool IsValidResourceHandle(in ResourceHandle handle);
    public Extent2D GetExtent(in ResourceHandle handle);
    public ImageFormat GetFormat(in ResourceHandle handle);
    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles);

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0);
    public ulong GetBufferAddress(in ResourceHandle handle);

    public Task CreateMesh<TVertexFormat>(out MeshHandle handle, IReadOnlyBuffer<TVertexFormat> vertices,
        IReadOnlyBuffer<uint> indices,
        IEnumerable<MeshSurface> surfaces) where TVertexFormat : unmanaged;

    public bool IsValidMeshHandle(in MeshHandle handle);
    public IMesh? GetMesh(in MeshHandle handle);
    public void FreeMeshHandles(params MeshHandle[] handles);
    public void Collect();
    public void Execute();

    public static IGraphicsModule Get()
    {
        return Global.Provider.Get<IGraphicsModule>();
    }
}