using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using rin.Graphics.Windows;
using TerraFX.Interop.Vulkan;
[assembly: DisableRuntimeMarshalling]

namespace rin.Graphics;

internal static partial class NativeMethods
{
    private const string DllName = "rin.GraphicsN";
    
    [LibraryImport(DllName, EntryPoint = "createVulkanInstance")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void CreateInstance(IntPtr inWindow, VkInstance * outInstance, VkDevice * outDevice,
        VkPhysicalDevice * outPhysicalDevice, VkQueue * outGraphicsQueue, uint * outGraphicsQueueFamily, VkQueue * outTransferQueue, uint * outTransferQueueFamily, VkSurfaceKHR * outSurface, VkDebugUtilsMessengerEXT * debugMessenger);

    [LibraryImport(DllName, EntryPoint = "destroyVulkanMessenger")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void DestroyMessenger(VkInstance instance,VkDebugUtilsMessengerEXT debugMessenger);
    
    [LibraryImport(DllName, EntryPoint = "createSurface")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void CreateSurface(VkInstance instance, IntPtr window,VkSurfaceKHR * surface);

    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void SwapchainCreatedDelegate(ulong swapchain, void* swapchainImages,
        uint numSwapchainImages, void* swapchainImageViews, uint numSwapchainImageViews);
    
    [LibraryImport(DllName, EntryPoint = "createSwapchain")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CreateSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,
        int swapchainFormat, int colorSpace, int presentMode, uint width, uint height,
        [MarshalAs(UnmanagedType.FunctionPtr)] SwapchainCreatedDelegate onCreatedDelegate);
    
    [LibraryImport(DllName, EntryPoint = "allocatorCopyToBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void CopyToBuffer(IntPtr allocator, void* allocation, void* data, ulong size,
        ulong offset);
    
    [LibraryImport(DllName, EntryPoint = "allocatorCreate")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial IntPtr CreateAllocator(VkInstance instance,VkDevice device,
        VkPhysicalDevice physicalDevice);

    [LibraryImport(DllName, EntryPoint = "allocatorDestroy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial void DestroyAllocator(IntPtr allocator);


    [LibraryImport(DllName, EntryPoint = "allocatorNewBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial  void AllocateBuffer(VkBuffer * buffer, void** allocation, ulong size,
        IntPtr allocator,
        int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
        int mapped, [MarshalAs(UnmanagedType.BStr)] string debugName);

    [LibraryImport(DllName, EntryPoint = "allocatorNewImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial  void AllocateImage(VkImage * image, void** allocation,
        VkImageCreateInfo * createInfo, IntPtr allocator, [MarshalAs(UnmanagedType.BStr)] string debugName);

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
    public static unsafe partial void vkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);
    
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
    public static unsafe partial void vkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint vertexBindingDescriptionCount, 
        VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint vertexAttributeDescriptionCount, 
        VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);
    
    [LibraryImport(DllName, EntryPoint = "dVkCmdSetRasterizationSamplesEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
        VkSampleCountFlags rasterizationSamples);

    [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToCoverageEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint alphaToCoverageEnable);

    [LibraryImport(DllName, EntryPoint = "dVkCmdSetAlphaToOneEnableEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint alphaToOneEnable);

    [LibraryImport(DllName, EntryPoint = "dVkCmdSetSampleMaskEXT")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe partial void vkCmdSetSampleMaskEXT(
        VkCommandBuffer commandBuffer, 
        VkSampleCountFlags samples, 
        uint* pSampleMask);
    
    
    
    [LibraryImport(DllName, EntryPoint = "getWindowMousePosition")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static unsafe partial void GetMousePosition(IntPtr window, double * x, double * y);
    
    [LibraryImport(DllName, EntryPoint = "setWindowMousePosition")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void SetMousePosition(IntPtr window,double x,double y);
    
    [LibraryImport(DllName, EntryPoint = "getWindowPosition")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static unsafe partial void GetWindowPosition(IntPtr window, int * x, int * y);
    
    [LibraryImport(DllName, EntryPoint = "setWindowPosition")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void SetWindowPosition(IntPtr window,int x,int y);

    [LibraryImport(DllName, EntryPoint = "getWindowPixelSize")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static unsafe partial void GetWindowPixelSize(IntPtr window, int * width, int * height);
    

    [LibraryImport(DllName, EntryPoint = "setWindowFullScreen")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void SetWindowFullscreen(IntPtr window, int fullscreen); 
    
    [LibraryImport(DllName, EntryPoint = "getWindowFullScreen")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial int GetWindowFullscreen(IntPtr window); 
    
    [LibraryImport(DllName, EntryPoint = "createWindow")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static  unsafe partial nint Create(int width, int height,[MarshalUsing(typeof(Utf8StringMarshaller))] string name,CreateOptions * options);

    [LibraryImport(DllName, EntryPoint = "destroyWindow")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial nint Destroy(IntPtr nativePtr);

    [LibraryImport(DllName, EntryPoint = "initWindows")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial int InitGlfw();

    [LibraryImport(DllName, EntryPoint = "stopWindows")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void StopGlfw();

    [LibraryImport(DllName, EntryPoint = "pollWindows")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void PollEvents();
    
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeKeyDelegate(nint window, int key, int scancode, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCursorDelegate(nint window, double x, double y);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeMouseButtonDelegate(nint window, int button, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeFocusDelegate(nint window, int focused);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeScrollDelegate(nint window, double dx, double dy);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeSizeDelegate(nint window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCloseDelegate(nint window);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeCharDelegate(nint window, uint code, int mods);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeMaximizedDelegate(nint window, int maximized);
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeRefreshDelegate(nint window);
    
    [LibraryImport(DllName, EntryPoint = "setWindowCallbacks")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void SetWindowCallbacks(nint nativePtr,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeKeyDelegate keyDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCursorDelegate cursorDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeMouseButtonDelegate mouseButtonDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeFocusDelegate focusDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeScrollDelegate scrollDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeSizeDelegate sizeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCloseDelegate closeDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeCharDelegate charDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeMaximizedDelegate maximizedDelegate,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeRefreshDelegate refreshDelegate);
}