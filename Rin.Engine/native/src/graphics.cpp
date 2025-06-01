#define VMA_IMPLEMENTATION
//#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "graphics.hpp"

VULKAN_HPP_DEFAULT_DISPATCH_LOADER_DYNAMIC_STORAGE
#include <vulkan/vulkan.hpp>
#include <VkBootstrap.h>
#include <iostream>
#include <slang.h>
#include "platform.hpp"
#ifdef RIN_PLATFORM_WINDOWS
#endif

#ifdef RIN_PLATFORM_LINUX
#include <vulkan/vulkan_wayland.h>
#endif


#define VK_DISPATCH_CHECKED(FUNCTION,...) \
if(VULKAN_HPP_DEFAULT_DISPATCHER.FUNCTION == nullptr) \
{ \
    std::cerr << #FUNCTION << " Was Not Loaded" << "\n"; \
} \
return VULKAN_HPP_DEFAULT_DISPATCHER.FUNCTION(__VA_ARGS__);

EXPORT_IMPL void createVulkanInstance(void * windowHandle, VkInstance* outInstance, VkDevice* outDevice,
    VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily, VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily,
    VkSurfaceKHR* outSurface,
    VkDebugUtilsMessengerEXT* outMessenger)
{

    VULKAN_HPP_DEFAULT_DISPATCHER.init();

    auto systemInfo = vkb::SystemInfo::get_system_info().value();


    vkb::InstanceBuilder builder{};


    builder
        .set_app_name("Rin Engine")
        .require_api_version(1,3,0)
#ifdef RIN_PLATFORM_WIN
        .enable_extension("VK_KHR_win32_surface")
#endif
#ifdef RIN_PLATFORM_LINUX
    .enable_extension(VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME)
#endif
        //.request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        .use_default_debug_messenger()
#endif
        .enable_extension("VK_KHR_surface");


    //builder.enable_extension(vk::EXTShaderObjectExtensionName);
    // if (systemInfo.is_extension_available(vk::EXTShaderObjectExtensionName))
    // {
    //     builder.enable_extension(vk::EXTShaderObjectExtensionName);
    // }
    // else
    // {
    //     std::cout << "Enabling shader object layer since device does not support it" << "\n";
    //     builder.enable_layer("VK_LAYER_KHRONOS_shader_object");
    // }


    auto instanceResult = builder.build();

    if(!instanceResult)
    {
        std::cerr << "Failed to create Vulkan instance: " << instanceResult.error().message() << "\n";
        throw std::runtime_error("");
    }

    auto vkbInstance = instanceResult.value();

    auto instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    *outMessenger = vkbInstance.debug_messenger;
#endif

    vk::PhysicalDeviceVulkan13Features features{};
    features.dynamicRendering = true;
    features.synchronization2 = true;

    vk::PhysicalDeviceVulkan12Features features12{};

    vk::PhysicalDeviceShaderDrawParametersFeatures drawParametersFeatures{};
    drawParametersFeatures.setShaderDrawParameters(true);
    features12
        .setBufferDeviceAddress(true)
        .setDescriptorIndexing(true)
        .setDescriptorBindingPartiallyBound(true)
        .setRuntimeDescriptorArray(true)
        .setDescriptorBindingSampledImageUpdateAfterBind(true)
        .setDescriptorBindingStorageImageUpdateAfterBind(true)
        .setDescriptorBindingStorageBufferUpdateAfterBind(true)
        .setDescriptorBindingVariableDescriptorCount(true)
        .setScalarBlockLayout(true)
        .setDrawIndirectCount(true)
        .setBufferDeviceAddress(true);

    VkSurfaceKHR surf = platformWindowCreateSurface(instance,windowHandle);
    vkb::PhysicalDeviceSelector selector{vkbInstance};

    //selector.add_required_extension(vk::EXTShaderObjectExtensionName);

    selector.set_minimum_version(1,3)
            .set_required_features_13(features)
            .set_required_features_12(features12)
            .set_surface(surf);
    selector
    // .add_required_extension_features(
    //     static_cast<VkPhysicalDeviceShaderObjectFeaturesEXT>(shaderObjectFeatures))
    .add_required_extension_features(static_cast<VkPhysicalDeviceShaderDrawParametersFeatures>(drawParametersFeatures));
    // if (systemInfo.is_extension_available(vk::EXTShaderObjectExtensionName))
    // {
    //     selector.add_required_extension_features(
    //                 static_cast<VkPhysicalDeviceShaderObjectFeaturesEXT>(shaderObjectFeatures));
    // }

    auto physicalDeviceResult = selector.select();

    if(!physicalDeviceResult)
    {
        std::cerr << "Failed to select vulkan physical device: " << physicalDeviceResult.error().message() << "\n";
        throw std::runtime_error("");
    }

    const vkb::PhysicalDevice& physicalDevice = physicalDeviceResult.value();

    //physicalDevice.enable_extension_if_present(vk::EXTShaderObjectExtensionName);
    vkb::DeviceBuilder deviceBuilder{physicalDevice};

    auto deviceResult = deviceBuilder.build();

    if(!deviceResult)
    {
        std::cerr << "Failed to build vulkan device: " << deviceResult.error().message() << "\n";
        throw std::runtime_error("");
    }

    const vkb::Device& vkbDevice = deviceResult.value();

    auto device = vkbDevice.device;

    auto gpu = physicalDevice.physical_device;

    *outGraphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();
    auto transfer = vkbDevice.get_queue(vkb::QueueType::transfer);
    auto hasTransferQueue = transfer.has_value();
    *outTransferQueue = hasTransferQueue ? transfer.value() : vkbDevice.get_queue(vkb::QueueType::graphics).value();
    *outGraphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).value();
    *outTransferQueueFamily = hasTransferQueue ? vkbDevice.get_queue_index(vkb::QueueType::transfer).value() : vkbDevice.get_queue_index(vkb::QueueType::graphics).value();

    try
    {
        VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Instance(instance));
        VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Device(device));
    }
    catch(std::exception& e)
    {
        std::cerr << "Failed to load vulkan functions " << e.what() << "\n";
        throw e;
    }

    *outInstance = instance;
    *outDevice = device;
    *outPhysicalDevice = gpu;
    *outSurface = surf;
}

EXPORT_IMPL void destroyVulkanMessenger(VkInstance instance, VkDebugUtilsMessengerEXT messenger)
{
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    vkb::destroy_debug_utils_messenger(instance,messenger);
    //inst.destroyDebugUtilsMessengerEXT(messengerCasted);
#endif
}

EXPORT_IMPL void createSwapchain(VkDevice device, VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, int swapchainFormat, int colorSpace,
    int presentMode, uint32_t width, uint32_t height,
    CreateSwapchainCallback callback)
{
    const auto sFormat = static_cast<vk::Format>(swapchainFormat);
    const auto sColorSpace = static_cast<vk::ColorSpaceKHR>(colorSpace);
    const auto sPresentMode = static_cast<VkPresentModeKHR>(presentMode);

    vkb::SwapchainBuilder swapchainBuilder{
        physicalDevice, device,
        surface
    };
    vkb::Swapchain vkbSwapchain = swapchainBuilder
                                  .set_desired_format(
                                      vk::SurfaceFormatKHR(
                                          sFormat,
                                          sColorSpace))
                                  // .set_desired_present_mode(
                                  //     VK_PRESENT_MODE_FIFO_KHR)
                                  .set_desired_present_mode(sPresentMode)
                                  .set_desired_extent(width,height)
                                  .add_image_usage_flags(
                                      VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                                  .build()
                                  .value();

    auto images = vkbSwapchain.get_images().value();
    auto views = vkbSwapchain.get_image_views().value();
    callback(reinterpret_cast<uintptr_t>(vkbSwapchain.swapchain),images.data(),images.size(),views.data(),
             views.size());
}

void createBuffer(VmaAllocator allocator, VkBuffer* buffer, VmaAllocation* allocation, const size_t allocSize,
    const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
    const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char* name)
{
    const auto bufferInfo = vk::BufferCreateInfo({},allocSize,
                                                 usage);
    //vma::AllocationCreateFlagBits::eMapped
    VmaAllocationCreateInfo vmaAllocInfo = {};
    vmaAllocInfo.flags = flags;
    vmaAllocInfo.usage = memoryUsage;
    vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);


    const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;

    vmaCreateBuffer(allocator,&vmaBufferCreateInfo,&vmaAllocInfo,buffer,allocation,
                    nullptr);
    vmaSetAllocationName(allocator,*allocation,name);
}

EXPORT_IMPL void* allocatorCreate(VkInstance instance, VkDevice device, VkPhysicalDevice physicalDevice)
{
    auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
    allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
    allocatorCreateInfo.device = (device);
    allocatorCreateInfo.physicalDevice = (physicalDevice);
    allocatorCreateInfo.instance = (instance);
    allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0,1,3,0);
    VmaAllocator allocator;
    vmaCreateAllocator(&allocatorCreateInfo,&allocator);
    return allocator;
}

EXPORT_IMPL void allocatorDestroy(void* allocator)
{
    vmaDestroyAllocator(static_cast<VmaAllocator>(allocator));
}

EXPORT_IMPL void allocatorNewBuffer(VkBuffer* buffer, void** allocation, size_t size, void* allocator,
    int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
    int mapped, const char* debugName)
{
    VmaAllocation alloc;
    VmaAllocationCreateFlags createFlags = sequentialWrite
                                               ? VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT
                                               : VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT;
    if(mapped)
    {
        createFlags |= VMA_ALLOCATION_CREATE_MAPPED_BIT;
    }

    createBuffer(static_cast<VmaAllocator>(allocator),buffer,&alloc,size,vk::BufferUsageFlags(usageFlags),
                 preferHost ? VMA_MEMORY_USAGE_AUTO_PREFER_HOST : VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                 vk::MemoryPropertyFlags(memoryPropertyFlags),
                 createFlags,debugName);

    *allocation = static_cast<void*>(alloc);
}

EXPORT_IMPL void allocatorNewImage(VkImage* image, void** allocation, VkImageCreateInfo* createInfo, void* allocator,
    const char* debugName)
{
    VmaAllocationCreateInfo imageAllocInfo = {};

    imageAllocInfo.usage = VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
    imageAllocInfo.requiredFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
    const auto actualAllocator = static_cast<VmaAllocator>(allocator);

    VmaAllocation alloc;
    auto result = vmaCreateImage(actualAllocator,createInfo,&imageAllocInfo,image,&alloc,
                                 nullptr);

    if(result != VK_SUCCESS)
    {
        throw std::runtime_error("Failed to create image");
    }

    vmaSetAllocationName(actualAllocator,alloc,debugName);
    *allocation = static_cast<void*>(alloc);
}

EXPORT_IMPL void allocatorFreeBuffer(VkBuffer buffer, void* allocation, void* allocator)
{
    vmaDestroyBuffer(static_cast<VmaAllocator>(allocator),buffer,
                     static_cast<VmaAllocation>(allocation));
}

EXPORT_IMPL void allocatorFreeImage(VkImage image, void* allocation, void* allocator)
{
    vmaDestroyImage(static_cast<VmaAllocator>(allocator),image,
                    static_cast<VmaAllocation>(allocation));
}

EXPORT_IMPL void allocatorCopyToBuffer(void* allocator, void* allocation, void* data, const size_t size,
    size_t offset)
{
    // auto dataCasted = static_cast<TestStruct*>(data);
    // std::cout << "SENDING TO SHADER " << dataCasted->viewport[2] << std::endl;
    vmaCopyMemoryToAllocation(static_cast<VmaAllocator>(allocator),data,static_cast<VmaAllocation>(allocation),
                              offset,size);
}

EXPORT_IMPL void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint32_t stageCount, VkShaderStageFlagBits* pStages,
    VkShaderEXT* pShaders)
{
    VK_DISPATCH_CHECKED(vkCmdBindShadersEXT,commandBuffer,stageCount,pStages,pShaders)
}

EXPORT_IMPL void dVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, VkRenderingInfo* pRenderingInfo)
{
    //VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBeginRenderingKHR(commandBuffer,pRenderingInfo);
    VK_DISPATCH_CHECKED(vkCmdBeginRenderingKHR,commandBuffer,pRenderingInfo)
}

EXPORT_IMPL void dVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode)
{
    VK_DISPATCH_CHECKED(vkCmdSetPolygonModeEXT,commandBuffer,polygonMode)
}

EXPORT_IMPL void dVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp)
{
    VK_DISPATCH_CHECKED(vkCmdSetLogicOpEXT,commandBuffer,logicOp)
}

EXPORT_IMPL void dVkCmdSetVertexInputEXT(VkCommandBuffer commandBuffer, uint32_t vertexBindingDescriptionCount,
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, uint32_t vertexAttributeDescriptionCount,
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions)
{
    VK_DISPATCH_CHECKED(vkCmdSetVertexInputEXT,commandBuffer,vertexBindingDescriptionCount,
                                                         pVertexBindingDescriptions,vertexAttributeDescriptionCount,
                                                         pVertexAttributeDescriptions)
}

EXPORT_IMPL void dVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable)
{
    VK_DISPATCH_CHECKED(vkCmdSetLogicOpEnableEXT,commandBuffer,logicOpEnable)
}

EXPORT_IMPL void dVkCmdSetColorBlendEnableEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, uint32_t* pColorBlendEnables)
{
    VK_DISPATCH_CHECKED(vkCmdSetColorBlendEnableEXT,commandBuffer,firstAttachment,attachmentCount,pColorBlendEnables)
}

EXPORT_IMPL void dVkCmdSetColorBlendEquationEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, VkColorBlendEquationEXT* pColorBlendEquations)
{
    VK_DISPATCH_CHECKED(vkCmdSetColorBlendEquationEXT,commandBuffer,firstAttachment,attachmentCount,pColorBlendEquations)
}

EXPORT_IMPL void dVkCmdSetColorWriteMaskEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, VkColorComponentFlags* pColorWriteMasks)
{
    VK_DISPATCH_CHECKED(vkCmdSetColorWriteMaskEXT,commandBuffer,firstAttachment,attachmentCount,pColorWriteMasks)
}

EXPORT_IMPL VkResult dVkCreateShadersEXT(VkDevice device, uint32_t createInfoCount, VkShaderCreateInfoEXT* pCreateInfos,
    VkAllocationCallbacks* pAllocator, VkShaderEXT* pShaders)
{
    VK_DISPATCH_CHECKED(vkCreateShadersEXT,device,createInfoCount,pCreateInfos,pAllocator,pShaders)
}

EXPORT_IMPL void dVkDestroyShaderEXT(VkDevice device, VkShaderEXT shader, VkAllocationCallbacks* pAllocator)
{
    VK_DISPATCH_CHECKED(vkDestroyShaderEXT,device,shader,pAllocator)
}

EXPORT_IMPL void dVkCmdSetRasterizationSamplesEXT(VkCommandBuffer commandBuffer, VkSampleCountFlagBits rasterizationSamples)
{
    VK_DISPATCH_CHECKED(vkCmdSetRasterizationSamplesEXT,commandBuffer,rasterizationSamples)
}

EXPORT_IMPL void dVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable)
{
    VK_DISPATCH_CHECKED(vkCmdSetAlphaToCoverageEnableEXT,commandBuffer,alphaToCoverageEnable)
}

EXPORT_IMPL void dVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable)
{
    VK_DISPATCH_CHECKED(vkCmdSetAlphaToOneEnableEXT,commandBuffer,alphaToOneEnable)
}

EXPORT_IMPL void dVkCmdSetSampleMaskEXT(VkCommandBuffer commandBuffer, VkSampleCountFlagBits samples, uint32_t* pSampleMask)
{
    VK_DISPATCH_CHECKED(vkCmdSetSampleMaskEXT,commandBuffer,samples,pSampleMask)
}
