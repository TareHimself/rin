using Rin.Framework.Graphics.Meshes;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Textures;
using Rin.Framework.Graphics.Windows;

namespace Rin.Framework.Graphics;

public interface IGraphicsModule : IModule, IUpdatable
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;

    public IWindowRenderer? GetWindowRenderer(IWindow window);
    public IRenderer[] GetRenderers();
    public IWindowRenderer[] GetWindowRenderers();
    public IShaderManager GetShaderManager();
    public IGraphicsShader MakeGraphics(string path);
    public IComputeShader MakeCompute(string path);
    public IImageFactory GetImageFactory();
    public IMeshFactory GetMeshFactory();

    public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
        IWindow? parent = null);

    public void WaitGraphicsIdle();

    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer");

    public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true)
        where T : unmanaged
    {
        return NewStorageBuffer(Utils.ByteSizeOf<T>(), sequentialWrite);
    }

    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true);

    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true);
    
    public IImage2D CreateDeviceImage(Extent3D size, ImageFormat format, ImageUsage usage, bool mips = false,
        string? debugName = null);

    public Task<IImage2D> CreateDeviceImage(IHostImage image, ImageUsage usage, bool mips = false,
        ImageFilter mipMapFilter = ImageFilter.Linear);

    public Task<IImage2D> CreateDeviceImage(Buffer<byte> content, Extent3D size, ImageFormat format,
        ImageUsage usage,
        bool mips = false, ImageFilter mipsGenerateFilter = ImageFilter.Linear);

    public void Collect();
    public void Execute();
}