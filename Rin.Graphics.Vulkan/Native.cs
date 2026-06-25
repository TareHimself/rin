using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using JetBrains.Annotations;
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
#elif OS_LINUX
    private const string DllName = "libRin.Graphics.Vulkan.Native";
#elif OS_FREEBSD
#elif OS_MAC
#endif
    
    [LibraryImport(DllName)]
    public static unsafe partial void createVulkanInstance(ulong windowHandle, VkInstance* outInstance,
        VkDevice* outDevice, VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue,
        uint* outGraphicsQueueFamily, VkQueue* outTransferQueue, uint* outTransferQueueFamily,
        VkSurfaceKHR* outSurface, VkDebugUtilsMessengerEXT* debugMessenger);

    [LibraryImport(DllName)]
    public static unsafe partial void destroyVulkanMessenger(VkInstance instance,
        VkDebugUtilsMessengerEXT debugMessenger);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorCopyToBuffer(IntPtr allocator, IntPtr allocation, IntPtr data,
        ulong size, ulong offset);

    [LibraryImport(DllName)]
    public static unsafe partial IntPtr allocatorCreate(VkInstance instance, VkDevice device,
        VkPhysicalDevice physicalDevice);

    [LibraryImport(DllName)]
    public static partial void allocatorDestroy(IntPtr allocator);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorNewBuffer(VkBuffer* buffer, ref IntPtr allocation, ulong size,
        IntPtr allocator, int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
        int mapped, [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorNewImage(ref VkImage image, ref IntPtr allocation,
        VkImageCreateInfo* createInfo, IntPtr allocator,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string debugName);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorFreeBuffer(VkBuffer buffer, IntPtr allocation, IntPtr allocator);

    [LibraryImport(DllName)]
    public static unsafe partial void allocatorFreeImage(VkImage image, IntPtr allocation, IntPtr allocator);

    [LibraryImport(DllName)]
    public static unsafe partial void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint stageCount,
        VkShaderStageFlags* pStages, VkShaderEXT* pShaders);
    

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionBuilderNew();

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetSpirv(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetGlsl(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddPreprocessorDefinition(void* builder,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string name,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string value);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddSearchPath(void* builder,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string path);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionBuilderBuild(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderFree(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionLoadModuleFromSourceString(void* session,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string moduleName,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string path,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string content, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionCreateComposedProgram(void* session, void* module,
        nuint* entryPoints, int entryPointsCount, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionFree(void* session);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangModuleFindEntryPointByName(void* module,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string name);

    [LibraryImport(DllName)]
    public static unsafe partial void slangEntryPointFree(void* entryPoint);

    [LibraryImport(DllName)]
    public static unsafe partial void slangModuleFree(void* module);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentGetEntryPointCode(void* component, int entryPointIndex,
        int targetIndex, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentLink(void* component, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentToLayoutJson(void* component);

    [LibraryImport(DllName)]
    public static unsafe partial void slangComponentFree(void* component);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangBlobNew();

    [LibraryImport(DllName)]
    public static unsafe partial int slangBlobGetSize(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangBlobGetPointer(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void slangBlobFree(void* blob);

    [LibraryImport(DllName)]
    public static partial void platformWindowPump();

    [LibraryImport(DllName)]
    public static partial ulong platformWindowCreate([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
        Extent2D extent, WindowFlags flags = WindowFlags.None);

    [LibraryImport(DllName)]
    public static partial void platformWindowDestroy(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowShow(ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowHide(ulong handle);

    [LibraryImport(DllName)]
    public static partial Extent2D platformWindowGetSize(ulong handle);

    [LibraryImport(DllName)]
    public static unsafe partial int platformWindowGetEvents(Window.WindowEvent* events, int size);

    [LibraryImport(DllName)]
    public static partial VkSurfaceKHR platformWindowCreateSurface(VkInstance instance, ulong handle);

    [LibraryImport(DllName)]
    public static partial void platformWindowSetCursorPosition(ulong handle, Vector2 position);

    [LibraryImport(DllName)]
    public static partial Vector2 platformWindowGetCursorPosition(ulong handle);

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
            Text,
            CursorFocus,
            KeyboardFocus,
            DndEnter,
            DndDrop,
            DndLeave
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
        public struct TextEvent
        {
            public EventType type;
            public ulong windowId;
            public char text;
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
            [FieldOffset(0)] public TextEvent text;
        }
    }
}