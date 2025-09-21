using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using TerraFX.Interop.Vulkan;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[assembly: DisableRuntimeMarshalling]

namespace Rin.Framework;

internal static partial class Native
{
#if OS_WINDOWS
    private const string DllName = "Rin.Framework.Native";
#elif OS_LINUX
    private const string DllName = "libRin.Framework.Native";
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
        public static unsafe partial void CreateInstance(ulong windowHandle, VkInstance* outInstance,
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
        public static unsafe partial void CopyToBuffer(IntPtr allocator, IntPtr allocation, IntPtr data, ulong size,
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
        public static unsafe partial void AllocateBuffer(VkBuffer* buffer, ref IntPtr allocation, ulong size,
            IntPtr allocator,
            int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
            int mapped, [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

        [LibraryImport(DllName, EntryPoint = "allocatorNewImage")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static unsafe partial void AllocateImage(ref VkImage image, ref IntPtr allocation,
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


    public static partial class Video
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void AudioCallbackDelegate(float* data, int count, double time);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate ulong SourceAvailableCallbackDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate ulong SourceLengthCallbackDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SourceReadCallbackDelegate(ulong position, ulong size, IntPtr destination);

        [LibraryImport(DllName, EntryPoint = "videoContextCreate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextCreate();

        [LibraryImport(DllName, EntryPoint = "videoContextHasVideo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextHasVideo(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetVideoExtent")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial Extent2D ContextGetVideoExtent(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSeek")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSeek(IntPtr context, double time);

        [LibraryImport(DllName, EntryPoint = "videoContextHasAudio")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextHasAudio(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSetAudioCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetAudioCallback(IntPtr context,
            [MarshalAs(UnmanagedType.FunctionPtr)] AudioCallbackDelegate callback);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioSampleRate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioSampleRate(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioChannels")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioChannels(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioTrackCount")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioTrackCount(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSetAudioTrack")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetAudioTrack(IntPtr context, int track);

        [LibraryImport(DllName, EntryPoint = "videoContextGetDuration")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial double ContextGetDuration(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetPosition")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial double ContextGetPosition(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSeek")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSeek(double time);

        [LibraryImport(DllName, EntryPoint = "videoContextDecode")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextDecode(IntPtr context, double delta);

        [LibraryImport(DllName, EntryPoint = "videoContextEnded")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextEnded(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextCopyRecentFrame")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextCopyRecentFrame(IntPtr context, double time);

        [LibraryImport(DllName, EntryPoint = "videoContextSetSource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetSource(IntPtr context, IntPtr source);

        [LibraryImport(DllName, EntryPoint = "videoContextFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoSourceCreate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr SourceCreate(
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceReadCallbackDelegate readCallback,
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceAvailableCallbackDelegate availableCallback,
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceLengthCallbackDelegate lengthCallback);

        [LibraryImport(DllName, EntryPoint = "videoSourceFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SourceFree(IntPtr source);
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
        public static partial void SelectFile([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple, [MarshalUsing(typeof(Utf8StringMarshaller))] string filter,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

        [LibraryImport(DllName, EntryPoint = "platformSelectPath")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SelectPath([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
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
                Close,
                Text,
                CursorFocus,
                KeyboardFocus,
                DndEnter,
                DndDrop,
                DndLeave
            }

            [LibraryImport(DllName, EntryPoint = "platformWindowCreate")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial ulong Create([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
                Extent2D extent, WindowFlags flags = WindowFlags.None);

            [LibraryImport(DllName, EntryPoint = "platformWindowDestroy")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Destroy(ulong handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowShow")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Show(ulong handle);


            [LibraryImport(DllName, EntryPoint = "platformWindowHide")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void Hide(ulong handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowGetSize")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial Extent2D GetSize(ulong handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowPump")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void PumpEvents();

            [LibraryImport(DllName, EntryPoint = "platformWindowGetEvents")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static unsafe partial int GetEvents(WindowEvent* events, int size);

            [LibraryImport(DllName, EntryPoint = "platformWindowCreateSurface")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial VkSurfaceKHR CreateSurface(VkInstance instance, ulong handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowSetCursorPosition")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void SetCursorPosition(ulong handle, Vector2 position);

            [LibraryImport(DllName, EntryPoint = "platformWindowGetCursorPosition")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial Vector2 GetCursorPosition(ulong handle);

            [LibraryImport(DllName, EntryPoint = "platformWindowSetSize")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
            public static partial void SetSize(ulong handle, Extent2D size);

            public struct EventInfo
            {
                public EventType type;
                public ulong windowId;
            }

            public struct KeyEvent
            {
                public EventType type;
                public ulong windowId;
                public InputKey key;
                public InputState state;
                public InputModifier modifier;
            }

            public struct ResizeEvent
            {
                public EventType type;
                public ulong windowId;
                public Extent2D size;
            }

            public struct MinimizeEvent
            {
                public EventType type;
                public ulong windowId;
            }

            public struct MaximizeEvent
            {
                public EventType type;
                public ulong windowId;
            }

            public struct ScrollEvent
            {
                public EventType type;
                public ulong windowId;
                public Vector2 position;
                public Vector2 delta;
            }

            public struct CursorMoveEvent
            {
                public EventType type;
                public ulong windowId;
                public Vector2 position;
            }

            public struct CursorButtonEvent
            {
                public EventType type;
                public ulong windowId;
                public CursorButton button;
                public InputState state;
                public InputModifier modifier;
            }

            public struct FocusEvent
            {
                public EventType type;
                public ulong windowId;
                public int focused;
            }

            public struct CloseEvent
            {
                public EventType type;
                public ulong windowId;
            }

            public struct TextEvent
            {
                public EventType type;
                public ulong windowId;
                public char text;
            }

            [StructLayout(LayoutKind.Explicit)]
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
}