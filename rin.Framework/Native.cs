using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using rin.Framework.Graphics;
using TerraFX.Interop.Vulkan;

namespace rin.Framework;

internal static partial class Native
{
    private const string DllName = "rin.Framework.Native";
    
    public static partial class Slang
    {
        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderNew")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangSessionBuilderNew();

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetSpirv")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionBuilderAddTargetSpirv(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetGlsl")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionBuilderAddTargetGlsl(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddPreprocessorDefinition")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionBuilderAddPreprocessorDefinition(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string value);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddSearchPath")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionBuilderAddSearchPath(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderBuild")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangSessionBuilderBuild(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionBuilderFree(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionLoadModuleFromSourceString")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangSessionLoadModuleFromSourceString(void* session,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string moduleName,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string content, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionCreateComposedProgram")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangSessionCreateComposedProgram(void* session, void* module,
            nuint* entryPoints, int entryPointsCount, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangSessionFree(void* session);

        [LibraryImport(DllName, EntryPoint = "slangModuleFindEntryPointByName")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangModuleFindEntryPointByName(void* module,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name);

        [LibraryImport(DllName, EntryPoint = "slangEntryPointFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangEntryPointFree(void* entryPoint);

        [LibraryImport(DllName, EntryPoint = "slangModuleFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangModuleFree(void* module);

        [LibraryImport(DllName, EntryPoint = "slangComponentGetEntryPointCode")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangComponentGetEntryPointCode(void* component, int entryPointIndex,
            int targetIndex, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentLink")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangComponentLink(void* component, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentToLayoutJson")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangComponentToLayoutJson(void* component);

        [LibraryImport(DllName, EntryPoint = "slangComponentFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangComponentFree(void* component);

        [LibraryImport(DllName, EntryPoint = "slangBlobNew")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangBlobNew();

        [LibraryImport(DllName, EntryPoint = "slangBlobGetSize")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial int SlangBlobGetSize(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobGetPointer")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SlangBlobGetPointer(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SlangBlobFree(void* blob);
    }

    public static partial class Vulkan
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate VkSurfaceKHR CreateSurfaceDelegate(VkInstance swapchain);


        [LibraryImport(DllName, EntryPoint = "createVulkanInstance")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void CreateInstance(byte** extensions, uint numExtensions,
            [MarshalAs(UnmanagedType.FunctionPtr)] CreateSurfaceDelegate createSurfaceDelegate, VkInstance* outInstance,
            VkDevice* outDevice,
            VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint* outGraphicsQueueFamily,
            VkQueue* outTransferQueue, uint* outTransferQueueFamily, VkSurfaceKHR* outSurface,
            VkDebugUtilsMessengerEXT* debugMessenger);

        [LibraryImport(DllName, EntryPoint = "destroyVulkanMessenger")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void
            DestroyMessenger(VkInstance instance, VkDebugUtilsMessengerEXT debugMessenger);

        [LibraryImport(DllName, EntryPoint = "allocatorCopyToBuffer")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void CopyToBuffer(IntPtr allocator, void* allocation, void* data, ulong size,
            ulong offset);

        [LibraryImport(DllName, EntryPoint = "allocatorCreate")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial IntPtr CreateAllocator(VkInstance instance, VkDevice device,
            VkPhysicalDevice physicalDevice);

        [LibraryImport(DllName, EntryPoint = "allocatorDestroy")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void DestroyAllocator(IntPtr allocator);


        [LibraryImport(DllName, EntryPoint = "allocatorNewBuffer")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void AllocateBuffer(VkBuffer* buffer, void** allocation, ulong size,
            IntPtr allocator,
            int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
            int mapped, [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

        [LibraryImport(DllName, EntryPoint = "allocatorNewImage")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void AllocateImage(VkImage* image, void** allocation,
            VkImageCreateInfo* createInfo, IntPtr allocator,
            [MarshalUsing(typeof(Utf8StringMarshaller))] string debugName);

        [LibraryImport(DllName, EntryPoint = "allocatorFreeBuffer")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void FreeBuffer(VkBuffer buffer, IntPtr allocation, IntPtr allocator);

        [LibraryImport(DllName, EntryPoint = "allocatorFreeImage")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void FreeImage(VkImage image, IntPtr allocation, IntPtr allocator);


        [LibraryImport(DllName, EntryPoint = "dVkCmdBindShadersEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdBindShadersEXT(VkCommandBuffer commandBuffer,
            uint stageCount,
            VkShaderStageFlags* pStages,
            VkShaderEXT* pShaders);

        [LibraryImport(DllName, EntryPoint = "dVkCreateShadersEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial VkResult vkCreateShadersEXT(
            VkDevice device,
            uint createInfoCount,
            VkShaderCreateInfoEXT* pCreateInfos,
            VkAllocationCallbacks* pAllocator,
            VkShaderEXT* pShaders);

        [LibraryImport(DllName, EntryPoint = "dVkDestroyShaderEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkDestroyShaderEXT(
            VkDevice device,
            VkShaderEXT shader,
            VkAllocationCallbacks* pAllocator);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetPolygonModeEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer,
            VkPolygonMode polygonMode);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetLogicOpEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void vkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetLogicOpEnableEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void vkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint logicOpEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorBlendEnableEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetColorBlendEnableEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            uint* pColorBlendEnables);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorBlendEquationEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetColorBlendEquationEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            VkColorBlendEquationEXT* pColorBlendEquations);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetColorWriteMaskEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetColorWriteMaskEXT(
            VkCommandBuffer commandBuffer,
            uint firstAttachment,
            uint attachmentCount,
            VkColorComponentFlags* pColorWriteMasks);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetVertexInputEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetVertexInputEXT(VkCommandBuffer commandBuffer,
            uint vertexBindingDescriptionCount,
            VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions,
            uint vertexAttributeDescriptionCount,
            VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetRasterizationSamplesEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetRasterizationSamplesEXT(VkCommandBuffer commandBuffer,
            VkSampleCountFlags rasterizationSamples);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToCoverageEnableEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer,
            uint alphaToCoverageEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToOneEnableEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void vkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer,
            uint alphaToOneEnable);

        [LibraryImport(DllName, EntryPoint = "dVkCmdSetSampleMaskEXT")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
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
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial IntPtr ContextNew();

        [LibraryImport(DllName, EntryPoint = "sdfContextFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextMoveTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextMoveTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextLineTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextLineTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextQuadraticBezierTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextQuadraticBezierTo(IntPtr context, ref Vector2 control,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextCubicBezierTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextCubicBezierTo(IntPtr context, ref Vector2 control1,
            ref Vector2 control2,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextEnd")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextEnd(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMSDF")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextGenerateMsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMTSDF")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextGenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);
    }
}