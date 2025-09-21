using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Meshes;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Meshes;
using Rin.Framework.Graphics.Windows;

namespace Rin.Framework.Graphics;

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
    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer");
    public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true)
        where T : unmanaged
    {
        return NewStorageBuffer(Utils.ByteSizeOf<T>(), sequentialWrite);
    }
    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true);
    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true);
    public IDisposableTexture CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public IDisposableTextureArray CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public IDisposableCubemap CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public Task<IDisposableTexture> CreateTexture(IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public Task<IDisposableTextureArray> CreateTextureArray(IReadOnlyBuffer<byte> data, in Extent2D extent,
        ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None);
    public Task<IDisposableCubemap> CreateCubemap(IReadOnlyBuffer<byte> data, in Extent2D extent, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public void CreateTexture(out ImageHandle handle,in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public void CreateTextureArray(out ImageHandle handle,in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public void CreateCubemap(out ImageHandle handle,in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public Task CreateTexture(out ImageHandle handle,IReadOnlyBuffer<byte> data,in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public Task CreateTextureArray(out ImageHandle handle,IReadOnlyBuffer<byte> data,in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public Task CreateCubemap(out ImageHandle handle,IReadOnlyBuffer<byte> data,in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);
    public bool IsValidImageHandle(in ImageHandle handle);
    public ITexture? GetTexture(in ImageHandle handle);
    public ITextureArray? GetTextureArray(in ImageHandle handle);
    public ICubemap? GetCubemap(in ImageHandle handle);
    public void FreeImageHandles(params ImageHandle[] handles);
    public Task CreateMesh<TVertexFormat>(out MeshHandle handle, IReadOnlyBuffer<TVertexFormat> vertices,
        IReadOnlyBuffer<uint> indices,
        IEnumerable<MeshSurface> surfaces) where TVertexFormat : unmanaged;
    public bool IsValidMeshHandle(in ImageHandle handle);
    public IMesh? GetMesh(in MeshHandle handle);
    public void FreeMeshHandles(params MeshHandle[] handles);
    public void Collect();
    public void Execute();
    public static IGraphicsModule Get() => SFramework.Provider.Get<IGraphicsModule>();
}