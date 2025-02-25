using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using rin.Framework.Core.Math;
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
        public static unsafe partial void* SessionBuilderNew();

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetSpirv")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionBuilderAddTargetSpirv(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddTargetGlsl")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionBuilderAddTargetGlsl(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddPreprocessorDefinition")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionBuilderAddPreprocessorDefinition(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string value);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderAddSearchPath")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionBuilderAddSearchPath(void* builder,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderBuild")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SessionBuilderBuild(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionBuilderFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionBuilderFree(void* builder);

        [LibraryImport(DllName, EntryPoint = "slangSessionLoadModuleFromSourceString")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SessionLoadModuleFromSourceString(void* session,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string moduleName,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string path,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string content, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionCreateComposedProgram")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* SessionCreateComposedProgram(void* session, void* module,
            nuint* entryPoints, int entryPointsCount, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangSessionFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void SessionFree(void* session);

        [LibraryImport(DllName, EntryPoint = "slangModuleFindEntryPointByName")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* ModuleFindEntryPointByName(void* module,
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string name);

        [LibraryImport(DllName, EntryPoint = "slangEntryPointFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void EntryPointFree(void* entryPoint);

        [LibraryImport(DllName, EntryPoint = "slangModuleFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void ModuleFree(void* module);

        [LibraryImport(DllName, EntryPoint = "slangComponentGetEntryPointCode")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* ComponentGetEntryPointCode(void* component, int entryPointIndex,
            int targetIndex, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentLink")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* ComponentLink(void* component, void* outDiagnostics);

        [LibraryImport(DllName, EntryPoint = "slangComponentToLayoutJson")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* ComponentToLayoutJson(void* component);

        [LibraryImport(DllName, EntryPoint = "slangComponentFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void ComponentFree(void* component);

        [LibraryImport(DllName, EntryPoint = "slangBlobNew")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* BlobNew();

        [LibraryImport(DllName, EntryPoint = "slangBlobGetSize")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial int BlobGetSize(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobGetPointer")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void* BlobGetPointer(void* blob);

        [LibraryImport(DllName, EntryPoint = "slangBlobFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe partial void BlobFree(void* blob);
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
            [MarshalUsing(typeof(Utf8StringMarshaller))]
            string debugName);

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

    public static partial class Math
    {
        [LibraryImport(DllName, EntryPoint = "mathQuatToRotator")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void QuatToRotator(ref Rotator result, ref Quat quat);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatVector4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyQuatVector(ref Vector3 result, ref Quat left,
            ref Vector3 right);

        [LibraryImport(DllName, EntryPoint = "mathQuatFromAngle")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void QuatFromAngle(ref Quat result, float angle, ref Vector3 axis);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatQuat")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyQuatQuat(ref Quat result, ref Quat left,
            ref Quat right);

        [LibraryImport(DllName, EntryPoint = "mathQuatToMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void QuatToMatrix4(ref Mat4 result, ref Quat target);

        [LibraryImport(DllName, EntryPoint = "mathQuatLookAt")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void QuatLookAt(ref Quat result, ref Vector3 from, ref Vector3 to, ref Vector3 up);

        [LibraryImport(DllName, EntryPoint = "mathGlmOrthographic")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void GlmOrthographic(ref Mat4 result, float left, float right, float bottom,
            float top);


        [LibraryImport(DllName, EntryPoint = "mathGlmPerspective")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void
            GlmPerspective(ref Mat4 result, float fov, float aspect, float near, float far);

        [LibraryImport(DllName, EntryPoint = "mathInverseMatrix3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Inverse(ref Mat3 result, ref Mat3 target);

        [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Translate(ref Mat3 result, ref Mat3 target, ref Vector2 translation);

        [LibraryImport(DllName, EntryPoint = "mathScaleMatrix3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Scale(ref Mat3 result, ref Mat3 target, ref Vector2 scale);

        [LibraryImport(DllName, EntryPoint = "mathRotateMatrix3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Rotate(ref Mat3 result, ref Mat3 target, float angle);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Matrix3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyMatrix3Matrix3(ref Mat3 result, ref Mat3 left, ref Mat3 right);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Vector3")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyMatrix3Vector3(ref Vector3 result, ref Mat3 left,
            ref Vector3 right);

        [LibraryImport(DllName, EntryPoint = "mathInverseMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Inverse(ref Mat4 result, ref Mat4 target);

        [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Translate(ref Mat4 result, ref Mat4 target, ref Vector3 translation);

        [LibraryImport(DllName, EntryPoint = "mathScaleMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Scale(ref Mat4 result, ref Mat4 target, ref Vector3 scale);

        [LibraryImport(DllName, EntryPoint = "mathRotateMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Rotate(ref Mat4 result, ref Mat4 target, float angle,
            ref Vector3 axis);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Matrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyMatrix4Matrix4(ref Mat4 result, ref Mat4 left, ref Mat4 right);

        [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Vector4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MultiplyMatrix4Vector4(ref Vector4 result, ref Mat4 left,
            ref Vector4 right);

        [LibraryImport(DllName, EntryPoint = "mathMatrix4ToTransform")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Matrix4ToTransform(ref Transform.NativeTransform result, ref Mat4 target);

        [LibraryImport(DllName, EntryPoint = "mathTransformToMatrix4")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void TransformToMatrix4(ref Mat4 result, ref Transform.NativeTransform target);
    }

    public static partial class Platform
    {
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void NativePathDelegate(char* path);
        
        [DllImport(DllName, EntryPoint = "platformInit", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Init();

        [LibraryImport(DllName, EntryPoint = "platformSelectFile")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void SelectFile([MarshalAs(UnmanagedType.BStr)] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple, [MarshalAs(UnmanagedType.BStr)] string filter,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

        [LibraryImport(DllName, EntryPoint = "platformSelectPath")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void SelectPath([MarshalAs(UnmanagedType.BStr)] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);
    }
}