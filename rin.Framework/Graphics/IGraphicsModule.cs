using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using rin.Framework.Core;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Slang;
using rin.Framework.Graphics.Windows;
using SDL;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface IGraphicsModule : IAppModule, IUpdatable
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnRendererCreated;
    public event Action<IWindowRenderer>? OnRendererDestroyed;

    public IWindowRenderer? GetWindowRenderer(IWindow window);

    public IRenderer[] GetRenderers();

    public IWindowRenderer[] GetWindowRenderers();
    
    public static uint MakeVulkanVersion(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 16) | patch;
    }
    
    public IShaderManager GetShaderManager();

    public IGraphicsShader GraphicsShaderFromPath(string path);

    public IComputeShader ComputeShaderFromPath(string path);

    public ITextureManager GetTextureManager();

    public IWindow CreateWindow(int width, int height, string name, CreateOptions? options = null,
        IWindow? parent = null
    );
    
    private static uint DeriveMipLevels(Extent2D extent)
    {
        return (uint)(Math.Floor(Math.Log2(Math.Max(extent.Width, extent.Height))) + 1);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer");

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true, string debugName = "storageBuffer")
        where T : unmanaged
    {
        return NewStorageBuffer(Core.Utils.ByteSizeOf<T>(), sequentialWrite, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Storage Buffer");

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Uniform Buffer");

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer<T>(bool sequentialWrite = true, string debugName = "uniformBuffer")
        where T : unmanaged
    {
        return NewUniformBuffer(Core.Utils.ByteSizeOf<T>(), sequentialWrite, debugName);
    }

    public IDeviceImage CreateImage(Extent3D size, ImageFormat format, ImageUsage usage, bool mipMap = false, string debugName = "Image");

    private static VkCommandBufferBeginInfo MakeCommandBufferBeginInfo(VkCommandBufferUsageFlags flags)
    {
        return new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
            flags = flags
        };
    }
    
    public Task TransferSubmit(Action<VkCommandBuffer> action);

    public Task GraphicsSubmit(Action<VkCommandBuffer> action);
    
    /// <summary>
    ///     Creates an image from the data in the native buffer
    /// </summary>
    /// <param name="content"></param>
    /// <param name="size"></param>
    /// <param name="format"></param>
    /// <param name="usage"></param>
    /// <param name="mips"></param>
    /// <param name="mipMapFilter"></param>
    /// <param name="debugName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task<IDeviceImage> CreateImage(NativeBuffer<byte> content, Extent3D size, ImageFormat format,
        ImageUsage usage,
        bool mips = false, ImageFilter mipMapFilter = ImageFilter.Linear,
        string debugName = "Image");

    /// <summary>
    ///     Call <see cref="IRenderer.Collect" /> for each <see cref="IRenderer" />
    /// </summary>
    public void Collect();

    /// <summary>
    ///     Resolve pending transfer submits and call <see cref="IRenderer.Execute" /> on all collected
    ///     <see cref="IRenderer" />
    /// </summary>
    public void Execute();
}