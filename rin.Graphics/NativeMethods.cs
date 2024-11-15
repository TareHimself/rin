using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Vulkan;
[assembly: DisableRuntimeMarshalling]

namespace rin.Graphics;

internal static partial class NativeMethods
{
    private const string DllName = "rin.GraphicsN";
    
    [LibraryImport(DllName, EntryPoint = "graphicsCreateVulkanInstance")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void CreateInstance(IntPtr inWindow, VkInstance * outInstance, VkDevice * outDevice,
        VkPhysicalDevice * outPhysicalDevice, VkQueue * outGraphicsQueue, uint * outGraphicsQueueFamily, VkQueue * outTransferQueue, uint * outTransferQueueFamily, VkSurfaceKHR * outSurface, VkDebugUtilsMessengerEXT * debugMessenger);

    [LibraryImport(DllName, EntryPoint = "graphicsDestroyVulkanMessenger")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void DestroyMessenger(VkInstance instance,VkDebugUtilsMessengerEXT debugMessenger);
    
    [LibraryImport(DllName, EntryPoint = "graphicsCreateSurface")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void CreateSurface(VkInstance instance, IntPtr window,VkSurfaceKHR * surface);

    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void SwapchainCreatedDelegate(ulong swapchain, void* swapchainImages,
        uint numSwapchainImages, void* swapchainImageViews, uint numSwapchainImageViews);
    
    [LibraryImport(DllName, EntryPoint = "graphicsCreateSwapchain")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CreateSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,
        int swapchainFormat, int colorSpace, int presentMode, uint width, uint height,
        [MarshalAs(UnmanagedType.FunctionPtr)] SwapchainCreatedDelegate onCreatedDelegate);
    
    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorCopyToBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void CopyToBuffer(IntPtr allocator, void* allocation, void* data, ulong size,
        ulong offset);
    
    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorCreate")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial IntPtr CreateAllocator(VkInstance instance,VkDevice device,
        VkPhysicalDevice physicalDevice);

    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorDestroy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial void DestroyAllocator(IntPtr allocator);


    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorNewBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial  void AllocateBuffer(VkBuffer * buffer, void** allocation, ulong size,
        IntPtr allocator,
        int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
        int mapped, [MarshalAs(UnmanagedType.BStr)] string debugName);

    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorNewImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial  void AllocateImage(VkImage * image, void** allocation,
        VkImageCreateInfo * createInfo, IntPtr allocator, [MarshalAs(UnmanagedType.BStr)] string debugName);

    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorFreeBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void FreeBuffer(VkBuffer buffer, IntPtr allocation, IntPtr allocator);

    [LibraryImport(DllName, EntryPoint = "graphicsAllocatorFreeImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void FreeImage(VkImage image, IntPtr allocation, IntPtr allocator);
    
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdBindShadersEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
        uint stageCount, 
        VkShaderStageFlags* pStages, 
        VkShaderEXT* pShaders);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCreateShadersEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial VkResult vkCreateShadersEXT(
        VkDevice device,
        uint createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);

    [LibraryImport(DllName, EntryPoint = "graphicsVkDestroyShaderEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetPolygonModeEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetLogicOpEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial void vkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetLogicOpEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial void vkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint logicOpEnable);
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetColorBlendEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetColorBlendEnableEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        uint* pColorBlendEnables);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetColorBlendEquationEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetColorBlendEquationEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        VkColorBlendEquationEXT* pColorBlendEquations);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetColorWriteMaskEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetColorWriteMaskEXT(
        VkCommandBuffer commandBuffer, 
        uint firstAttachment, 
        uint attachmentCount, 
        VkColorComponentFlags* pColorWriteMasks);
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetVertexInputEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint vertexBindingDescriptionCount, 
        VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint vertexAttributeDescriptionCount, 
        VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);
    
    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetRasterizationSamplesEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
        VkSampleCountFlags rasterizationSamples);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetAlphaToCoverageEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint alphaToCoverageEnable);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetAlphaToOneEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint alphaToOneEnable);

    [LibraryImport(DllName, EntryPoint = "graphicsVkCmdSetSampleMaskEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetSampleMaskEXT(
        VkCommandBuffer commandBuffer, 
        VkSampleCountFlags samples, 
        uint* pSampleMask);
}