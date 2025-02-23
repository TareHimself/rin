using System.Text;
using System.Text.RegularExpressions;
using rin.Framework.Core;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Slang;
using rin.Framework.Graphics.Windows;
using SDL;
using TerraFX.Interop.Vulkan;
using static SDL.SDL3;

namespace rin.Framework.Graphics;

public partial class GraphicsModule : IGraphicsModule
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnRendererCreated;
    public event Action<IWindowRenderer>? OnRendererDestroyed;

    private App? _app;
    public App GetApp() => _app ?? throw new NullReferenceException();

    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly DescriptorLayoutFactory _descriptorLayoutFactory = new();
    private readonly List<Pair<TaskCompletionSource, Action<VkCommandBuffer>>> _pendingGraphicsSubmits = [];
    private readonly List<Pair<TaskCompletionSource, Action<VkCommandBuffer>>> _pendingTransferSubmits = [];
    private readonly List<IRenderer> _renderers = [];
    private readonly Dictionary<SamplerSpec, VkSampler> _samplers = [];
    private readonly Mutex _samplersMutex = new();
    private readonly BackgroundTaskQueue _transferQueueThread = new();
    private readonly Dictionary<IWindow, IWindowRenderer> _windows = [];
    private IntPtr _allocator = IntPtr.Zero;
    private IRenderContext[] _collected = [];
    private VkDebugUtilsMessengerEXT _debugUtilsMessenger;
    private DescriptorAllocator? _descriptorAllocator;
    private VkDevice _device;
    private VkCommandBuffer _graphicsCommandBuffer;
    private VkCommandPool _graphicsCommandPool;
    private VkFence _graphicsFence;
    private VkQueue _graphicsQueue;
    private uint _graphicsQueueFamily;
    private bool _hasDedicatedTransferQueue;
    private VkInstance _instance;
    private VkPhysicalDevice _physicalDevice;
    private IShaderManager? _shaderCompiler;
    private Dictionary<nuint, SdlWindow> _sdlWindows = [];

    private VkSurfaceFormatKHR _surfaceFormat = new()
    {
        colorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR,
        format = VkFormat.VK_FORMAT_R8G8B8A8_UNORM
    };

    private ITextureManager? _textureManager;
    private VkCommandBuffer _transferCommandBuffer;
    private VkCommandPool _transferCommandPool;
    private VkFence _transferFence;
    private VkQueue _transferQueue;
    private uint _transferQueueFamily;
    private int _maxEventsPerPeep = 64;
    
    [GeneratedRegex("VK_FORMAT_R[0-9]{1,2}G[0-9]{1,2}B[0-9]{1,2}A[0-9]{1,2}_UNORM")]
    private static partial Regex SurfaceFormatRegex();
    
    public VkInstance GetInstance()
    {
        return _instance;
    }

    public VkDevice GetDevice()
    {
        return _device;
    }

    public VkQueue GetGraphicsQueue()
    {
        return _graphicsQueue;
    }

    public uint GetGraphicsQueueFamily()
    {
        return _graphicsQueueFamily;
    }

    public VkQueue GetTransferQueue()
    {
        return _transferQueue;
    }

    public uint GetTransferQueueFamily()
    {
        return _transferQueueFamily;
    }

    public VkPhysicalDevice GetPhysicalDevice()
    {
        return _physicalDevice;
    }
    
    private IWindow Internal_CreateWindow(int width, int height, string name, CreateOptions? options = null,
        IWindow? parent = null
    )
    {
        unsafe
        {
            var opts = options.GetValueOrDefault(new CreateOptions());

            SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_VULKAN;

            if (opts.Resizable)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
            }

            if (!opts.Visible)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
            }

            if (!opts.Decorated)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }

            if (opts.Focused)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS;
            }

            if (opts.Floating)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
            }

            if (opts.Transparent)
            {
                flags |= SDL_WindowFlags.SDL_WINDOW_TRANSPARENT;
            }

            fixed (byte* data = Encoding.UTF8.GetBytes(name))
            {
                var windowPtr = SDL_CreateWindow(data, width, height, flags);

                var win = new SdlWindow(windowPtr, parent);

                _sdlWindows.Add((nuint)windowPtr, win);

                win.OnDisposed += () => { _sdlWindows.Remove((nuint)windowPtr); };

                return win;
            }
        }
    }
    
    private unsafe void InitVulkan()
    {
        var outInstance = _instance;
        var outDevice = _device;
        var outPhysicalDevice = _physicalDevice;
        var outGraphicsQueue = _graphicsQueue;
        uint outGraphicsQueueFamily = 0;
        var outTransferQueue = _graphicsQueue;
        uint outTransferQueueFamily = 0;
        var outSurface = new VkSurfaceKHR();
        var outDebugMessenger = _debugUtilsMessenger;

        // We create a window just for surface information
        using var window = Internal_CreateWindow(1, 1, "Graphics Init Window", new CreateOptions
        {
            Visible = false,
            Decorated = false,
            Resizable = false
        });

        Update(0);

        if (window == null) throw new Exception("Failed to create window to init graphics");
        uint extensionCount = 0;
        var extensions = SDL_Vulkan_GetInstanceExtensions(&extensionCount);
        Native.Vulkan.CreateInstance(extensions, extensionCount, (inst) =>
            {
                unsafe
                {
                    VkSurfaceKHR surface = new();
                    var surfValuePtr = &surface.Value;
                    // ReSharper disable once AccessToDisposedClosure
                    var windowPtr = (SDL_Window*)window.GetPtr();
                    SDL_Vulkan_CreateSurface(windowPtr, (SDL.VkInstance_T*)inst.Value, null,
                        (SDL.VkSurfaceKHR_T**)surfValuePtr);
                    return surface;
                }
            }, &outInstance, &outDevice, &outPhysicalDevice, &outGraphicsQueue,
            &outTransferQueueFamily, &outTransferQueue, &outTransferQueueFamily, &outSurface, &outDebugMessenger);
        _instance = outInstance;
        _device = outDevice;
        _physicalDevice = outPhysicalDevice;
        _graphicsQueue = outGraphicsQueue;
        _graphicsQueueFamily = outGraphicsQueueFamily;
        _transferQueue = outTransferQueue;
        _transferQueueFamily = outTransferQueueFamily;
        _debugUtilsMessenger = outDebugMessenger;
        _hasDedicatedTransferQueue = _graphicsQueue != _transferQueue;

        var formats = _physicalDevice.GetSurfaceFormats(outSurface).Where(c =>
        {
            var asString = c.format.ToString();
            return SurfaceFormatRegex().IsMatch(asString);
        }).ToArray();

        _surfaceFormat = formats.FirstOrDefault(_surfaceFormat);

        _descriptorAllocator = new DescriptorAllocator(10000, [
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, 3),
            new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 4)
        ]);
        _transferFence = _device.CreateFence();
        _transferCommandPool = _device.CreateCommandPool(GetTransferQueueFamily());
        _transferCommandBuffer = _device.AllocateCommandBuffers(_transferCommandPool).First();
        _graphicsFence = _device.CreateFence();
        _graphicsCommandPool = _device.CreateCommandPool(GetGraphicsQueueFamily());
        _graphicsCommandBuffer = _device.AllocateCommandBuffers(_graphicsCommandPool).First();

        _allocator = _allocator = Native.Vulkan.CreateAllocator(GetInstance(), GetDevice(), GetPhysicalDevice());
        _shaderCompiler = new SlangShaderManager();
        _textureManager = new TextureManager();
        _instance.DestroySurface(outSurface);
    }
    
    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" />
    /// </summary>
    public IDeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags,
        bool sequentialWrite = true, bool preferHost = false, bool mapped = false, string debugName = "Buffer")
    {
        unsafe
        {
            VkBuffer buffer = new();
            void* allocation;
            Native.Vulkan.AllocateBuffer(&buffer, &allocation, size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);
            var result = new DeviceBuffer(buffer, size, this, (IntPtr)allocation, debugName);
            return result;
        }
    }


    /// <summary>
    ///     Allocates a <see cref="DeviceImage" />
    /// </summary>
    public IDeviceImage NewDeviceImage(VkImageCreateInfo imageCreateInfo, string debugName = "Image")
    {
        unsafe
        {
            VkImage image = new();
            void* allocation;
            Native.Vulkan.AllocateImage(&image, &allocation, &imageCreateInfo, _allocator, debugName);
            var result = new DeviceImage(image, new VkImageView(), new Extent3D
                {
                    Width = imageCreateInfo.extent.width,
                    Height = imageCreateInfo.extent.height,
                    Dimensions = imageCreateInfo.extent.depth
                },
                imageCreateInfo.format.FromVk(), this,
                (IntPtr)allocation, debugName);
            return result;
        }
    }

    /// <summary>
    ///     Free's a <see cref="DeviceBuffer" />
    /// </summary>
    public void FreeBuffer(DeviceBuffer buffer)
    {
        Native.Vulkan.FreeBuffer(buffer.NativeBuffer, buffer.Allocation, _allocator);
    }

    /// <summary>
    ///     Free's a <see cref="DeviceImage" />
    /// </summary>
    public void FreeImage(DeviceImage image)
    {
        unsafe
        {
            vkDestroyImageView(_module.GetDevice(), image.NativeView, null);
            Native.Vulkan.FreeImage(image.NativeImage, image.Allocation, _allocator);
        }
    }
    
    public void Start(App app)
    {
        _app = app;
        
        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD))
            throw new InvalidOperationException($"failed to initialise SDL. Error: {SDL_GetError()}");

        InitVulkan();
    }

    public void Stop(App app)
    {
        lock (_renderers)
        {
            _renderers.Clear();
        }

        foreach (var (window, renderer) in _windows)
        {
            OnWindowClosed?.Invoke(window);
            OnRendererDestroyed?.Invoke(renderer);
            window.Dispose();
        }

        _windows.Clear();
        _backgroundTaskQueue.Dispose();
        _transferQueueThread.Dispose();
        _descriptorAllocator?.Dispose();
        _shaderCompiler?.Dispose();
        _textureManager?.Dispose();
        _descriptorLayoutFactory.Dispose();

        lock (_samplersMutex)
        {
            foreach (var sampler in _samplers.Values) _device.DestroySampler(sampler);

            _samplers.Clear();
        }
        
        Native.Vulkan.DestroyAllocator(_allocator);
        _allocator = IntPtr.Zero;
        _device.DestroyCommandPool(_graphicsCommandPool);
        _device.DestroyFence(_graphicsFence);
        _device.DestroyCommandPool(_transferCommandPool);
        _device.DestroyFence(_transferFence);
        _device.Destroy();
        if (_debugUtilsMessenger.Value != 0) Native.Vulkan.DestroyMessenger(_instance, _debugUtilsMessenger);
        _instance.Destroy();

        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD);
    }

    public void Update(float deltaSeconds)
    {
        SDL_PumpEvents();

        unsafe
        {
            var events = stackalloc SDL_Event[_maxEventsPerPeep];
            var eventsPumped = 0;
            do
            {
                eventsPumped = SDL_PeepEvents(events, _maxEventsPerPeep, SDL_EventAction.SDL_GETEVENT,
                    (uint)SDL_EventType.SDL_EVENT_FIRST, (uint)SDL_EventType.SDL_EVENT_LAST);
                for (var i = 0; i < eventsPumped; i++)
                {
                    var e = events + i;
                    var windowPtr = SDL_GetWindowFromEvent(e);
                    if (windowPtr == null) continue;
                    var window = _sdlWindows[(nuint)windowPtr];
                    window.HandleEvent(*e);
                }
            } while (eventsPumped == _maxEventsPerPeep);
        }
    }
    
    public IWindowRenderer? GetWindowRenderer(IWindow window)
    {
        throw new NotImplementedException();
    }

    public IRenderer[] GetRenderers()
    {
        throw new NotImplementedException();
    }

    public IWindowRenderer[] GetWindowRenderers()
    {
        throw new NotImplementedException();
    }

    public IShaderManager GetShaderManager()
    {
        throw new NotImplementedException();
    }

    public IGraphicsShader GraphicsShaderFromPath(string path)
    {
        throw new NotImplementedException();
    }

    public IComputeShader ComputeShaderFromPath(string path)
    {
        throw new NotImplementedException();
    }

    public ITextureManager GetTextureManager()
    {
        throw new NotImplementedException();
    }

    public IWindow CreateWindow(int width, int height, string name, CreateOptions? options = null, IWindow? parent = null)
    {
        throw new NotImplementedException();
    }

    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true, string debugName = "Transfer Buffer")
    {
        throw new NotImplementedException();
    }

    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true, string debugName = "Storage Buffer")
    {
        throw new NotImplementedException();
    }

    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true, string debugName = "Uniform Buffer")
    {
        throw new NotImplementedException();
    }

    public IDeviceImage CreateImage(Extent3D size, ImageFormat format, ImageUsage usage, bool mipMap = false,
        string debugName = "Image")
    {
        throw new NotImplementedException();
    }

    public Task TransferSubmit(Action<VkCommandBuffer> action)
    {
        throw new NotImplementedException();
    }

    public Task GraphicsSubmit(Action<VkCommandBuffer> action)
    {
        throw new NotImplementedException();
    }

    public Task<IDeviceImage> CreateImage(NativeBuffer<byte> content, Extent3D size, ImageFormat format, ImageUsage usage, bool mips = false,
        ImageFilter mipMapFilter = ImageFilter.Linear, string debugName = "Image")
    {
        throw new NotImplementedException();
    }

    public void Collect()
    {
        throw new NotImplementedException();
    }

    public void Execute()
    {
        throw new NotImplementedException();
    }
}