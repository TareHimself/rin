using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Windows;
using TerraFX.Interop.Vulkan;

[assembly: DisableRuntimeMarshalling]

namespace Rin.Engine;

internal static partial class Native
{
#if OS_WINDOWS
    private const string DllName = "Rin.Engine.Native";
#elif OS_LINUX
    private const string DllName = "libRin.Engine.Native";
#elif OS_FREEBSD
#elif OS_MAC
#endif


    public static partial class Memory
    {
        [LibraryImport(DllName, EntryPoint = "memoryAllocate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr Allocate(ulong size);

        [LibraryImport(DllName, EntryPoint = "memorySet")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Set(IntPtr ptr, int value, ulong size);

        [LibraryImport(DllName, EntryPoint = "memoryFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Free(IntPtr ptr);
    }

    public static partial class Slang
    {
        // [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        // public unsafe delegate int LoadFileDelegate(byte** data);
        //
        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderNew")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* SessionBuilderNew(delegate*<byte*, byte**, int> loadFileDelegate);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetSpirv")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionBuilderAddTargetSpirv(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetGlsl")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionBuilderAddTargetGlsl(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddPreprocessorDefinition")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionBuilderAddPreprocessorDefinition(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string value);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddSearchPath")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionBuilderAddSearchPath(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderBuild")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* SessionBuilderBuild(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionBuilderFree(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionLoadModuleFromSourceString")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* SessionLoadModuleFromSourceString(void* session,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string moduleName,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string content, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionCreateComposedProgram")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* SessionCreateComposedProgram(void* session, void* module,
            nuint* entryPoints, int entryPointsCount, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void SessionFree(void* session);

        [LibraryImport(DllName, EntryPoint = "slangModuleFindEntryPointByName")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* ModuleFindEntryPointByName(void* module,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name);

        [LibraryImport(DllName, EntryPoint = "slangEntryPointFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void EntryPointFree(void* entryPoint);

        [LibraryImport(DllName, EntryPoint = "slangModuleFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void ModuleFree(void* module);

        [LibraryImport(DllName, EntryPoint = "slangComponentGetEntryPointCode")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* ComponentGetEntryPointCode(void* component, int entryPointIndex,
            int targetIndex, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentLink")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* ComponentLink(void* component, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentToLayoutJson")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* ComponentToLayoutJson(void* component);

        [LibraryImport(DllName, EntryPoint = "slangComponentFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void ComponentFree(void* component);

        [LibraryImport(DllName, EntryPoint = "slangBlobNew")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* BlobNew();

        [LibraryImport(DllName, EntryPoint = "slangBlobGetSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial int BlobGetSize(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobGetPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void* BlobGetPointer(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void BlobFree(void* blob);
    }

    public static partial class Vulkan
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate VkSurfaceKHR CreateSurfaceDelegate(VkInstance swapchain);


        [LibraryImport(DllName, EntryPoint = "createVulkanInstance")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void CreateInstance(IntPtr windowHandle, VkInstance* outInstance,
            VkDevice* outDevice,
            VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint* outGraphicsQueueFamily,
            VkQueue* outTransferQueue, uint* outTransferQueueFamily, VkSurfaceKHR* outSurface,
            VkDebugUtilsMessengerEXT* debugMessenger);

        [LibraryImport(DllName, EntryPoint = "destroyVulkanMessenger")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void
            DestroyMessenger(VkInstance instance, VkDebugUtilsMessengerEXT debugMessenger);

        [LibraryImport(DllName, EntryPoint = "allocatorCopyToBuffer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void CopyToBuffer(IntPtr allocator, void* allocation, void* data, ulong size,
            ulong offset);

        [LibraryImport(DllName, EntryPoint = "allocatorCreate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial IntPtr CreateAllocator(VkInstance instance, VkDevice device,
            VkPhysicalDevice physicalDevice);

        [LibraryImport(DllName, EntryPoint = "allocatorDestroy")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void DestroyAllocator(IntPtr allocator);


        [LibraryImport(DllName, EntryPoint = "allocatorNewBuffer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void AllocateBuffer(VkBuffer* buffer, void** allocation, ulong size,
            IntPtr allocator,
            int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
            int mapped, [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

        [LibraryImport(DllName, EntryPoint = "allocatorNewImage")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void AllocateImage(VkImage* image, void** allocation,
            VkImageCreateInfo* createInfo, IntPtr allocator,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string debugName);

        [LibraryImport(DllName, EntryPoint = "allocatorFreeBuffer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void FreeBuffer(VkBuffer buffer, IntPtr allocation, IntPtr allocator);

        [LibraryImport(DllName, EntryPoint = "allocatorFreeImage")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void FreeImage(VkImage image, IntPtr allocation, IntPtr allocator);


        [LibraryImport(DllName, EntryPoint = "dVkCmdBindShadersEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdBindShadersEXT(VkCommandBuffer commandBuffer,
            uint stageCount,
            VkShaderStageFlags* pStages,
            VkShaderEXT* pShaders);

        [LibraryImport(DllName, EntryPoint = "dVkCreateShadersEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial VkResult vkCreateShadersEXT(
            VkDevice device,
            uint createInfoCount,
            VkShaderCreateInfoEXT* pCreateInfos,
            VkAllocationCallbacks* pAllocator,
            VkShaderEXT* pShaders);

        [LibraryImport(DllName, EntryPoint = "dVkDestroyShaderEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkDestroyShaderEXT(
            VkDevice device,
            VkShaderEXT shader,
            VkAllocationCallbacks* pAllocator);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetPolygonModeEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer,
            VkPolygonMode polygonMode);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetLogicOpEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void vkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetLogicOpEnableEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void vkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint logicOpEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorBlendEnableEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetColorBlendEnableEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            uint* pColorBlendEnables);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorBlendEquationEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetColorBlendEquationEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            VkColorBlendEquationEXT* pColorBlendEquations);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorWriteMaskEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetColorWriteMaskEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            VkColorComponentFlags* pColorWriteMasks);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetVertexInputEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetVertexInputEXT(VkCommandBuffer commandBuffer,
            uint vertexBindingDescriptionCount,
            VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions,
            uint vertexAttributeDescriptionCount,
            VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetRasterizationSamplesEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetRasterizationSamplesEXT(VkCommandBuffer commandBuffer,
            VkSampleCountFlags rasterizationSamples);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToCoverageEnableEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer,
            uint alphaToCoverageEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToOneEnableEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer,
            uint alphaToOneEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetSampleMaskEXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void vkCmdSetSampleMaskEXT(
            VkCommandBuffer commandBuffer,
            VkSampleCountFlags samples,
            uint* pSampleMask);
    }

    public static partial class Sdf
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GenerateDelegate(IntPtr data, uint pixelWidth, uint pixelHeight, uint byteSize,
            double width, double height);

        [LibraryImport(DllName, EntryPoint = "sdfContextNew")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextNew();

        [LibraryImport(DllName, EntryPoint = "sdfContextFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextMoveTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextMoveTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextLineTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextLineTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextQuadraticBezierTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextQuadraticBezierTo(IntPtr context, ref Vector2 control,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextCubicBezierTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextCubicBezierTo(IntPtr context, ref Vector2 control1,
            ref Vector2 control2,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextEnd")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextEnd(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMSDF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextGenerateMsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMTSDF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextGenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);
    }

    public static partial class Platform
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void NativePathDelegate(char* path);

        [LibraryImport(DllName, EntryPoint = "platformInit")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Init();

        [LibraryImport(DllName, EntryPoint = "platformShutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Shutdown();

        [LibraryImport(DllName, EntryPoint = "platformSelectFile")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SelectFile([MarshalAs(UnmanagedType.BStr)] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple, [MarshalAs(UnmanagedType.BStr)] string filter,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

        [LibraryImport(DllName, EntryPoint = "platformSelectPath")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SelectPath([MarshalAs(UnmanagedType.BStr)] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

        public static partial class Window
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
                CursorEnter,
                CursorLeave,
                Focus,
                Close,
                Text
            }

            [LibraryImport(DllName, EntryPoint = "platformWindowCreate")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial IntPtr Create([MarshalUsing(typeof(Utf8StringMarshaller))] string title, int width,
                int height, WindowFlags flags = WindowFlags.None);

            [LibraryImport(DllName, EntryPoint = "platformWindowDestroy")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Destroy(IntPtr handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowShow")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Show(IntPtr handle);


            [LibraryImport(DllName, EntryPoint = "platformWindowHide")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Hide(IntPtr handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowGetSize")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial Extent2D GetSize(IntPtr handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowPump")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void PumpEvents();

            [LibraryImport(DllName, EntryPoint = "platformWindowGetEvents")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static unsafe partial int GetEvents(WindowEvent* events, int size);

            [LibraryImport(DllName, EntryPoint = "platformWindowCreateSurface")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial VkSurfaceKHR CreateSurface(VkInstance instance, IntPtr handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowSetCursorPosition")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void SetCursorPosition(IntPtr handle, Vector2 position);

            [LibraryImport(DllName, EntryPoint = "platformWindowGetCursorPosition")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial Vector2 GetCursorPosition(IntPtr handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowSetSize")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void SetSize(IntPtr handle, Extent2D size);

            [StructLayout(LayoutKind.Sequential)]
            public struct Info
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct KeyEvent
            {
                public EventType type;
                public IntPtr handle;
                public InputKey key;
                public InputState state;
                public InputModifier modifier;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct ResizeEvent
            {
                public EventType type;
                public IntPtr handle;
                public Extent2D size;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MinimizeEvent
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MaximizeEvent
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct ScrollEvent
            {
                public EventType type;
                public IntPtr handle;
                public Vector2 position;
                public Vector2 delta;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CursorMoveEvent
            {
                public EventType type;
                public IntPtr handle;
                public Vector2 position;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CursorButtonEvent
            {
                public EventType type;
                public IntPtr handle;
                public CursorButton button;
                public InputState state;
                public InputModifier modifier;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CursorEnterEvent
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CursorLeaveEvent
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct FocusEvent
            {
                public EventType type;
                public IntPtr handle;
                public int focused;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CloseEvent
            {
                public EventType type;
                public IntPtr handle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TextEvent
            {
                public EventType type;
                public IntPtr handle;
                public char text;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct WindowEvent
            {
                [FieldOffset(0)] public Info info;
                [FieldOffset(0)] public KeyEvent key;
                [FieldOffset(0)] public ResizeEvent resize;
                [FieldOffset(0)] public MinimizeEvent minimize;
                [FieldOffset(0)] public MaximizeEvent maximize;
                [FieldOffset(0)] public ScrollEvent scroll;
                [FieldOffset(0)] public CursorMoveEvent cursorMove;
                [FieldOffset(0)] public CursorButtonEvent cursorButton;
                [FieldOffset(0)] public CursorEnterEvent enter;
                [FieldOffset(0)] public CursorLeaveEvent leave;
                [FieldOffset(0)] public FocusEvent focus;
                [FieldOffset(0)] public CloseEvent close;
                [FieldOffset(0)] public TextEvent text;
            }
        }
    }
}