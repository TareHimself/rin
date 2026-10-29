using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using TerraFX.Interop.Vulkan;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[assembly: DisableRuntimeMarshalling]

namespace Rin.Graphics.Vulkan;

internal static partial class Native
{
#if OS_WINDOWS
    private const string DllName = "Rin.Graphics.Vulkan.Native";
#else
    private const string DllName = "libRin.Graphics.Vulkan.Native";
#endif
    
    [NoReorder]
    [StructLayout(LayoutKind.Sequential)]
    public struct VulkanInitResult
    {
        public VkInstance instance;
        public VkPhysicalDevice physicalDevice;
        public VkDevice device;
        public VkQueue graphicsQueue;
        public VkQueue transferQueue;
        public VkQueue computeQueue;
        public uint graphicsQueueFamily;
        public uint transferQueueFamily;
        public uint computeQueueFamily;
        public VkSurfaceKHR surface;
        public VkDebugUtilsMessengerEXT messenger;
        public bool supportsIndirectRendering;
        public bool supportsDedicatedCompute;
    }

    // Mirrors GraphicsResult in native/Rin.Graphics.Vulkan.Native/src/result.hpp. Keep the two in sync.
    public enum GraphicsResult
    {
        Ok = 0,
        InvalidArgument,
        InstanceCreationFailed,
        NoSuitableDevice,
        DeviceCreationFailed,
        FunctionLoadingFailed,
        WindowCreationFailed,
        SurfaceCreationFailed,
        OutOfHostMemory,
        OutOfDeviceMemory,
        DeviceLost,
        VulkanError,
        PlatformError
    }

    private static void Check(GraphicsResult result, string function)
    {
        if (result != GraphicsResult.Ok) throw new NativeException(DllName, function, result);
    }

    public static unsafe VulkanInitResult CreateVulkanInstance(ulong windowHandle)
    {
        var result = new VulkanInitResult();
        Check(createVulkanInstance(windowHandle, &result), nameof(createVulkanInstance));
        return result;
    }

    public static unsafe IntPtr CreateAllocator(VkInstance instance, VkDevice device, VkPhysicalDevice physicalDevice)
    {
        var allocator = IntPtr.Zero;
        Check(allocatorCreate(instance, device, physicalDevice, &allocator), nameof(allocatorCreate));
        return allocator;
    }

    public static unsafe void NewBuffer(VkBuffer* buffer, ref IntPtr allocation, ulong size, IntPtr allocator,
        bool sequentialWrite, bool preferHost, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags,
        bool mapped, string debugName)
    {
        Check(allocatorNewBuffer(buffer, ref allocation, size, allocator, sequentialWrite ? 1 : 0, preferHost ? 1 : 0,
            (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName), nameof(allocatorNewBuffer));
    }

    public static unsafe void NewImage(ref VkImage image, ref IntPtr allocation, VkImageCreateInfo* createInfo,
        IntPtr allocator, string debugName)
    {
        Check(allocatorNewImage(ref image, ref allocation, createInfo, allocator, debugName), nameof(allocatorNewImage));
    }

    public static void CopyToBuffer(IntPtr allocator, IntPtr allocation, IntPtr data, ulong size, ulong offset)
    {
        Check(allocatorCopyToBuffer(allocator, allocation, data, size, offset), nameof(allocatorCopyToBuffer));
    }

    public static unsafe ulong CreateWindow(string title, Extent2D extent, WindowFlags flags)
    {
        ulong handle = 0;
        Check(platformWindowCreate(title, extent, flags, &handle), nameof(platformWindowCreate));
        return handle;
    }

    public static unsafe VkSurfaceKHR CreateSurface(VkInstance instance, ulong windowHandle)
    {
        VkSurfaceKHR surface = default;
        Check(platformWindowCreateSurface(instance, windowHandle, &surface), nameof(platformWindowCreateSurface));
        return surface;
    }

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult createVulkanInstance(ulong windowHandle, VulkanInitResult* outResult);

    [LibraryImport(DllName)]
    public static unsafe partial void destroyVulkanMessenger(VkInstance instance,
        VkDebugUtilsMessengerEXT debugMessenger);

    [LibraryImport(DllName)]
    private static partial GraphicsResult allocatorCopyToBuffer(IntPtr allocator, IntPtr allocation, IntPtr data,
        ulong size, ulong offset);

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult allocatorCreate(VkInstance instance, VkDevice device,
        VkPhysicalDevice physicalDevice, IntPtr* outAllocator);

    [LibraryImport(DllName)]
    public static partial void allocatorDestroy(IntPtr allocator);

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult allocatorNewBuffer(VkBuffer* buffer, ref IntPtr allocation, ulong size,
        IntPtr allocator, int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
        int mapped, [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult allocatorNewImage(ref VkImage image, ref IntPtr allocation,
        VkImageCreateInfo* createInfo, IntPtr allocator,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string debugName);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorSetAllocationName(IntPtr allocator, IntPtr allocation,
        [MarshalUsing(typeof(Utf8StringMarshaller))] string name);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorFreeBuffer(VkBuffer buffer, IntPtr allocation, IntPtr allocator);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorFreeImage(VkImage image, IntPtr allocation, IntPtr allocator);

    [LibraryImport(DllName)]
    public static unsafe partial void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint stageCount,
        VkShaderStageFlags* pStages, VkShaderEXT* pShaders);

    [LibraryImport(DllName)]
    public static partial void platformWindowPump();

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult platformWindowCreate(
        [MarshalUsing(typeof(Utf8StringMarshaller))] string title, Extent2D extent, WindowFlags flags,
        ulong* outHandle);

    [LibraryImport(DllName)]
    public static partial void platformWindowDestroy(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowShow(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowHide(ulong handle);

    [LibraryImport(DllName)]
    public static partial Extent2D platformWindowGetSize(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowSetFullscreen(ulong handle,
        [MarshalAs(UnmanagedType.U1)] bool fullscreen);

    [LibraryImport(DllName)]
    public static partial void platformWindowSetSize(ulong handle, Extent2D size);

    [LibraryImport(DllName)]
    public static unsafe partial int platformWindowGetEvents(Window.WindowEvent* events, int size);

    [LibraryImport(DllName)]
    private static unsafe partial GraphicsResult platformWindowCreateSurface(VkInstance instance, ulong handle,
        VkSurfaceKHR* outSurface);

    [LibraryImport(DllName)]
    public static partial void platformWindowSetCursorPosition(ulong handle, Vector2 position);

    [LibraryImport(DllName)]
    public static partial Vector2 platformWindowGetCursorPosition(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowStartTextInput(ulong handle, Window.Rect2D caret);

    [LibraryImport(DllName)]
    public static partial void platformWindowStopTextInput(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowSetTextInputRect(ulong handle, Window.Rect2D caret);

    [LibraryImport(DllName)]
    public static unsafe partial char* platformWindowGetEventText(Window.TextRef text);

    [LibraryImport(DllName)]
    public static unsafe partial void platformWindowSetDropCallbacks(ulong handle, Window.DropCallbacks* callbacks);

    [LibraryImport(DllName)]
    public static partial void platformWindowClearDropCallbacks(ulong handle);

    [LibraryImport(DllName)]
    public static partial int platformHasClipboardText();

    [LibraryImport(DllName)]
    public static unsafe partial char* platformGetClipboardText();

    [LibraryImport(DllName)]
    public static unsafe partial int platformSetClipboardText(char* text, ulong length);

    [LibraryImport(DllName)]
    public static partial void platformClearClipboard();

    public static class Window
    {
        public enum EventType : uint
        {
            Key,
            Resize,
            Minimize,
            Maximize,
            Scroll,
            CursorMove,
            CursorButton,
            Close,
            TextCommit,
            CursorFocus,
            KeyboardFocus,
            TextPreedit
        }

        [NoReorder]
        public struct Rect2D
        {
            public int x;
            public int y;
            public uint width;
            public uint height;
        }

        // Where an event's text lives, in UTF-16 units. Resolved by platformWindowGetEventText.
        [NoReorder]
        public struct TextRef
        {
            public uint offset;
            public uint length;
        }

        [NoReorder]
        public struct EventInfo
        {
            public EventType type;
            public ulong windowId;
        }

        [NoReorder]
        public struct KeyEvent
        {
            public EventType type;
            public ulong windowId;
            public InputKey key;
            public InputState state;
            public InputModifier modifier;
        }

        [NoReorder]
        public struct ResizeEvent
        {
            public EventType type;
            public ulong windowId;
            public Extent2D size;
        }

        [NoReorder]
        public struct MinimizeEvent
        {
            public EventType type;
            public ulong windowId;
        }

        [NoReorder]
        public struct MaximizeEvent
        {
            public EventType type;
            public ulong windowId;
        }

        [NoReorder]
        public struct ScrollEvent
        {
            public EventType type;
            public ulong windowId;
            public Vector2 position;
            public Vector2 delta;
        }

        [NoReorder]
        public struct CursorMoveEvent
        {
            public EventType type;
            public ulong windowId;
            public Vector2 position;
        }

        [NoReorder]
        public struct CursorButtonEvent
        {
            public EventType type;
            public ulong windowId;
            public CursorButton button;
            public InputState state;
            public InputModifier modifier;
        }

        [NoReorder]
        public struct FocusEvent
        {
            public EventType type;
            public ulong windowId;
            public int focused;
        }

        [NoReorder]
        public struct CloseEvent
        {
            public EventType type;
            public ulong windowId;
        }

        [NoReorder]
        public struct TextCommitEvent
        {
            public EventType type;
            public ulong windowId;
            public TextRef text;
        }

        [NoReorder]
        public struct TextPreeditEvent
        {
            public EventType type;
            public ulong windowId;
            public TextRef text;
            public uint caretStart;
            public uint caretEnd;
        }

        // Text is UTF-16 and files are UTF-8 paths, both only valid inside the callback.
        [NoReorder]
        public unsafe struct DropData
        {
            public char* text;
            public ulong textLength;
            public byte** files;
            public ulong fileCount;
        }

        [NoReorder]
        public struct DropInfo
        {
            public Vector2 position;
            public DropEffect allowed;
            public InputModifier modifiers;
        }

        [NoReorder]
        public unsafe struct DropCallbacks
        {
            public delegate* unmanaged<DropInfo*, DropData*, IntPtr, DropEffect> enter;
            public delegate* unmanaged<DropInfo*, DropData*, IntPtr, DropEffect> over;
            public delegate* unmanaged<DropInfo*, DropData*, IntPtr, DropEffect> drop;
            public delegate* unmanaged<IntPtr, void> leave;
            public IntPtr userData;
        }

        [StructLayout(LayoutKind.Explicit)]
        [NoReorder]
        public struct WindowEvent
        {
            [FieldOffset(0)] public EventInfo info;
            [FieldOffset(0)] public KeyEvent key;
            [FieldOffset(0)] public ResizeEvent resize;
            [FieldOffset(0)] public MinimizeEvent minimize;
            [FieldOffset(0)] public MaximizeEvent maximize;
            [FieldOffset(0)] public ScrollEvent scroll;
            [FieldOffset(0)] public CursorMoveEvent cursorMove;
            [FieldOffset(0)] public CursorButtonEvent cursorButton;
            [FieldOffset(0)] public FocusEvent cursorFocus;
            [FieldOffset(0)] public FocusEvent keyboardFocus;
            [FieldOffset(0)] public CloseEvent close;
            [FieldOffset(0)] public TextCommitEvent textCommit;
            [FieldOffset(0)] public TextPreeditEvent textPreedit;
        }
    }
}