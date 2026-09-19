using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;

namespace Rin.Graphics.Null;

public sealed class NullGraphicsModule : IGraphicsModule
{
    private readonly List<IRenderer> _renderers = [];
    private readonly Dictionary<ResourceHandle, (Extent2D Extent, ImageFormat Format)> _textures = [];
    private readonly HashSet<ResourceHandle> _buffers = [];

    private uint _nextId = 1;

    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;

    public void Start(IApplication app)
    {
    }

    public void Stop(IApplication app)
    {
    }

    public void Update(float deltaTime)
    {
    }

    public void AddRenderer(IRenderer renderer)
    {
        _renderers.Add(renderer);
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        _renderers.Remove(renderer);
    }

    public IWindowRenderer? GetWindowRenderer(IWindow window)
    {
        return null;
    }

    public IRenderer[] GetRenderers()
    {
        return _renderers.ToArray();
    }

    public IWindowRenderer[] GetWindowRenderers()
    {
        return [];
    }

    public IGraphicsShader MakeGraphics(string path)
    {
        return new NullGraphicsShader();
    }

    public IComputeShader MakeCompute(string path)
    {
        return new NullComputeShader();
    }

    public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
        IWindow? parent = null)
    {
        var window = new NullWindow(name, extent, parent);
        OnWindowCreated?.Invoke(window);
        return window;
    }

    public void WaitIdle()
    {
    }

    public DeviceBufferView NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer")
    {
        return NewBuffer(size);
    }

    public DeviceBufferView NewStorageBuffer(ulong size, bool sequentialWrite = true)
    {
        return NewBuffer(size);
    }

    public DeviceBufferView NewUniformBuffer(ulong size, bool sequentialWrite = true)
    {
        return NewBuffer(size);
    }

    private DeviceBufferView NewBuffer(ulong size)
    {
        var handle = new ResourceHandle(ResourceType.Buffer, _nextId++);
        _buffers.Add(handle);
        return new DeviceBufferView(handle, 0, size);
    }

    public ResourceHandle CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        return TrackTexture(ResourceType.Texture, extent, format);
    }

    public ResourceHandle CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        return TrackTexture(ResourceType.TextureArray, extent, format);
    }

    public ResourceHandle CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        return TrackTexture(ResourceType.Cubemap, extent, format);
    }

    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        handle = TrackTexture(ResourceType.Texture, extent, format);
        return Task.FromResult(handle);
    }

    public Task<ResourceHandle> CreateTextureArray(out ResourceHandle handle, ReadOnlySpan<byte> data,
        in Extent2D extent, ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        handle = TrackTexture(ResourceType.TextureArray, extent, format);
        return Task.FromResult(handle);
    }

    public Task<ResourceHandle> CreateCubemap(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        handle = TrackTexture(ResourceType.Cubemap, extent, format);
        return Task.FromResult(handle);
    }

    public Task UploadToTexture(ResourceHandle handle, ReadOnlyMemory<byte> data, Extent2D extent,
        Offset2D offset = default)
    {
        return Task.CompletedTask;
    }

    private ResourceHandle TrackTexture(ResourceType type, Extent2D extent, ImageFormat format)
    {
        var handle = new ResourceHandle(type, _nextId++);
        _textures[handle] = (extent, format);
        return handle;
    }

    public bool IsValidResourceHandle(in ResourceHandle handle)
    {
        return _textures.ContainsKey(handle) || _buffers.Contains(handle);
    }

    public Extent2D GetExtent(in ResourceHandle handle)
    {
        return _textures.TryGetValue(handle, out var info) ? info.Extent : Extent2D.Zero;
    }

    public ImageFormat GetFormat(in ResourceHandle handle)
    {
        return _textures.TryGetValue(handle, out var info) ? info.Format : ImageFormat.RGBA8;
    }

    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles)
    {
        foreach (var handle in handles)
        {
            _textures.Remove(handle);
            _buffers.Remove(handle);
        }
    }

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0)
    {
    }

    public ulong GetBufferAddress(in ResourceHandle handle)
    {
        return handle.Id;
    }

    public void Collect()
    {
    }

    public void Execute()
    {
    }
}
