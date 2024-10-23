#define VMA_IMPLEMENTATION
//#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "graphics.hpp"
VULKAN_HPP_DEFAULT_DISPATCH_LOADER_DYNAMIC_STORAGE
//#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>
#include <vulkan/vulkan.hpp>
#include <VkBootstrap.h>
#include <iostream>


void graphicsCreateVulkanInstance(void* inWindow, void** outInstance, void** outDevice, void** outPhysicalDevice,
                                  void** outQueue, uint32_t* outQueueFamily, uintptr_t* outSurface,
                                  uintptr_t* outMessenger)
{

    VULKAN_HPP_DEFAULT_DISPATCHER.init();
    
    uint32_t numExtensions = 0;
    auto extensions = glfwGetRequiredInstanceExtensions(&numExtensions);
    auto systemInfo = vkb::SystemInfo::get_system_info().value();


    vkb::InstanceBuilder builder{};


    builder
        .set_app_name("Aerox")
        .require_api_version(1, 3, 0)

        //.request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        .use_default_debug_messenger()
#endif
        .enable_extensions(numExtensions, extensions);
    

    builder.enable_layer("VK_LAYER_KHRONOS_shader_object");
    
    if (systemInfo.is_extension_available(vk::EXTShaderObjectExtensionName))
    {
        builder.enable_extension(vk::EXTShaderObjectExtensionName);
    }
    
   

    auto instanceResult = builder.build();
    
    if (!instanceResult)
    {
        std::cerr << "Failed to create Vulkan instance: " << instanceResult.error().message() << "\n";
        throw std::runtime_error("");
    }
    
    auto vkbInstance = instanceResult.value();

    auto instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    *outMessenger = reinterpret_cast<uintptr_t>(vkbInstance.debug_messenger);
#endif

    vk::PhysicalDeviceShaderObjectFeaturesEXT shaderObjectFeatures{};
    shaderObjectFeatures.setShaderObject(true);

    vk::PhysicalDeviceVulkan13Features features{};
    features.dynamicRendering = true;
    features.synchronization2 = true;

    vk::PhysicalDeviceVulkan12Features features12{};
    features12.setBufferDeviceAddress(true)
        .setDescriptorIndexing(true)
        .setDescriptorBindingPartiallyBound(true)
        .setRuntimeDescriptorArray(true)
        .setDescriptorBindingSampledImageUpdateAfterBind(true)
        .setDescriptorBindingStorageImageUpdateAfterBind(true)
        .setDescriptorBindingStorageBufferUpdateAfterBind(true)
        .setDescriptorBindingVariableDescriptorCount(true)
        .setScalarBlockLayout(true);
    
    VkSurfaceKHR surf;
    glfwCreateWindowSurface(instance, static_cast<GLFWwindow*>(inWindow), nullptr, &surf);
    vkb::PhysicalDeviceSelector selector{vkbInstance};

    selector.add_required_extension(vk::EXTShaderObjectExtensionName);
    
    selector.set_minimum_version(1, 3)
                .set_required_features_13(features)
                .set_required_features_12(features12)
                .set_surface(surf);
    selector.add_required_extension_features(
                    static_cast<VkPhysicalDeviceShaderObjectFeaturesEXT>(shaderObjectFeatures));
    // if (systemInfo.is_extension_available(vk::EXTShaderObjectExtensionName))
    // {
    //     selector.add_required_extension_features(
    //                 static_cast<VkPhysicalDeviceShaderObjectFeaturesEXT>(shaderObjectFeatures));
    // }

    auto physicalDeviceResult = selector.select();
    
    if (!physicalDeviceResult)
    {
        std::cerr << "Failed to select vulkan physical device: " << physicalDeviceResult.error().message() << "\n";
        throw std::runtime_error("");
    }

    vkb::PhysicalDevice physicalDevice = physicalDeviceResult.value();
    
    physicalDevice.enable_extension_if_present(vk::EXTShaderObjectExtensionName);
    vkb::DeviceBuilder deviceBuilder{physicalDevice};

    auto deviceResult = deviceBuilder.build();

    if (!deviceResult)
    {
        std::cerr << "Failed to build vulkan device: " << deviceResult.error().message() << "\n";
        throw std::runtime_error("");
    }

    vkb::Device vkbDevice = deviceResult.value();

    auto device = vkbDevice.device;

    auto gpu = physicalDevice.physical_device;

    auto graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

    auto graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).value();

    try
    {
        VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Instance(instance));
        VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Device(device));
        // // Create your instance...
        // VULKAN_HPP_DEFAULT_DISPATCHER.init<vk::Instance>(instance);
        // // Create your device...
        // VULKAN_HPP_DEFAULT_DISPATCHER.init<vk::Device>(device);
    }
    catch (std::exception& e)
    {
        std::cerr << "Failed to load vulkan functions " << e.what() << "\n";
        throw e;
    }

    *outInstance = static_cast<void*>(instance);
    *outDevice = static_cast<void*>(device);
    *outPhysicalDevice = static_cast<void*>(gpu);
    *outQueue = static_cast<void*>(graphicsQueue);
    *outQueueFamily = graphicsQueueFamily;
    *outSurface = reinterpret_cast<uintptr_t>(surf);
}

void graphicsDestroyVulkanMessenger(void* instance, uintptr_t messenger)
{
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    const auto messengerCasted = reinterpret_cast<VkDebugUtilsMessengerEXT>(messenger);
    vkb::destroy_debug_utils_messenger(static_cast<VkInstance>(instance), messengerCasted);
    //inst.destroyDebugUtilsMessengerEXT(messengerCasted);
#endif
}

void graphicsCreateSwapchain(void* device, void* physicalDevice, uintptr_t surface, int swapchainFormat, int colorSpace,
                             int presentMode, uint32_t width, uint32_t height,
                             CreateSwapchainCallback callback)
{
    const auto sFormat = static_cast<vk::Format>(swapchainFormat);
    const auto sColorSpace = static_cast<vk::ColorSpaceKHR>(colorSpace);
    const auto sPresentMode = static_cast<VkPresentModeKHR>(presentMode);

    vkb::SwapchainBuilder swapchainBuilder{
        static_cast<VkPhysicalDevice>(physicalDevice), static_cast<VkDevice>(device),
        reinterpret_cast<VkSurfaceKHR>(surface)
    };
    vkb::Swapchain vkbSwapchain = swapchainBuilder
                                  .set_desired_format(
                                      vk::SurfaceFormatKHR(
                                          sFormat,
                                          sColorSpace))
                                  // .set_desired_present_mode(
                                  //     VK_PRESENT_MODE_FIFO_KHR)
                                  .set_desired_present_mode(sPresentMode)
                                  .set_desired_extent(width, height)
                                  .add_image_usage_flags(
                                      VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                                  .build()
                                  .value();

    auto images = vkbSwapchain.get_images().value();
    auto views = vkbSwapchain.get_image_views().value();
    callback(reinterpret_cast<uintptr_t>(vkbSwapchain.swapchain), images.data(), images.size(), views.data(),
             views.size());
}

void createBuffer(VmaAllocator allocator, VkBuffer* buffer, VmaAllocation* allocation, const size_t allocSize,
                  const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char* name)
{
    const auto bufferInfo = vk::BufferCreateInfo({}, allocSize,
                                                 usage);
    //vma::AllocationCreateFlagBits::eMapped
    VmaAllocationCreateInfo vmaAllocInfo = {};
    vmaAllocInfo.flags = flags;
    vmaAllocInfo.usage = memoryUsage;
    vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);


    const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;

    vmaCreateBuffer(allocator, &vmaBufferCreateInfo, &vmaAllocInfo, buffer, allocation,
                    nullptr);
    vmaSetAllocationName(allocator, *allocation, name);     
}

void* graphicsAllocatorCreate(void* instance, void* device, void* physicalDevice)
{
    auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
    allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
    allocatorCreateInfo.device = static_cast<VkDevice>(device);
    allocatorCreateInfo.physicalDevice = static_cast<VkPhysicalDevice>(physicalDevice);
    allocatorCreateInfo.instance = static_cast<VkInstance>(instance);
    allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0, 1, 3, 0);
    VmaAllocator allocator;
    vmaCreateAllocator(&allocatorCreateInfo, &allocator);
    return allocator;
}

void graphicsAllocatorDestroy(void* allocator)
{
    vmaDestroyAllocator(static_cast<VmaAllocator>(allocator));
}

void graphicsAllocatorNewBuffer(uintptr_t* buffer, void** allocation, unsigned long size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName)
{
    VmaAllocation alloc;
    VkBuffer buff;
    VmaAllocationCreateFlags createFlags = sequentialWrite
                                               ? VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT
                                               : VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT;
    if (mapped)
    {
        createFlags |= VMA_ALLOCATION_CREATE_MAPPED_BIT;
    }

    createBuffer(static_cast<VmaAllocator>(allocator), &buff, &alloc, size, vk::BufferUsageFlags(usageFlags),
                 preferHost ? VMA_MEMORY_USAGE_AUTO_PREFER_HOST : VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                 vk::MemoryPropertyFlags(memoryPropertyFlags),
                 createFlags, debugName);

    *allocation = static_cast<void*>(alloc);
    *buffer = reinterpret_cast<uintptr_t>(buff);
}

void graphicsAllocatorNewImage(uintptr_t* image, void** allocation, void* createInfo, void* allocator,
                               const char* debugName)
{
    VmaAllocationCreateInfo imageAllocInfo = {};

    imageAllocInfo.usage = VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
    imageAllocInfo.requiredFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
    const auto actualAllocator = static_cast<VmaAllocator>(allocator);
    const auto vmaImageCreateInfo = static_cast<VkImageCreateInfo*>(createInfo);

    VkImage vmaImage;
    VmaAllocation alloc;
    auto result = vmaCreateImage(actualAllocator, vmaImageCreateInfo, &imageAllocInfo, &vmaImage, &alloc,
                                 nullptr);

    if (result != VK_SUCCESS)
    {
        throw std::runtime_error("Failed to create image");
    }

    vmaSetAllocationName(actualAllocator, alloc, debugName);

    *image = reinterpret_cast<uintptr_t>(vmaImage);
    *allocation = static_cast<void*>(alloc);
}

void graphicsAllocatorFreeBuffer(uintptr_t buffer, void* allocation, void* allocator)
{
    vmaDestroyBuffer(static_cast<VmaAllocator>(allocator), reinterpret_cast<VkBuffer>(buffer),
                     static_cast<VmaAllocation>(allocation));
}

void graphicsAllocatorFreeImage(uintptr_t image, void* allocation, void* allocator)
{
    vmaDestroyImage(static_cast<VmaAllocator>(allocator), reinterpret_cast<VkImage>(image),
                    static_cast<VmaAllocation>(allocation));
}

void graphicsAllocatorCopyToBuffer(void* allocator, void* allocation, void* data, const unsigned long size,
                                   unsigned long offset)
{
    // auto dataCasted = static_cast<TestStruct*>(data);
    // std::cout << "SENDING TO SHADER " << dataCasted->viewport[2] << std::endl;
    vmaCopyMemoryToAllocation(static_cast<VmaAllocator>(allocator), data, static_cast<VmaAllocation>(allocation),
                              offset, size);
}

void graphicsVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint32_t stageCount, VkShaderStageFlagBits* pStages,
    VkShaderEXT* pShaders)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBindShadersEXT == nullptr)
    {
        std::cerr << "vkCmdBindShadersEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBindShadersEXT(commandBuffer,stageCount,pStages,pShaders);
}

void graphicsVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, VkRenderingInfo* pRenderingInfo)
{
    //VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBeginRenderingKHR(commandBuffer,pRenderingInfo);
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBeginRenderingKHR == nullptr)
    {
        std::cerr << "vkCmdBeginRenderingKHR Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdBeginRenderingKHR(commandBuffer,pRenderingInfo);
}

void graphicsVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetPolygonModeEXT == nullptr)
    {
        std::cerr << "vkCmdSetPolygonModeEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetPolygonModeEXT(commandBuffer,polygonMode);
}

void graphicsVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetLogicOpEXT == nullptr)
    {
        std::cerr << "vkCmdSetLogicOpEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetLogicOpEXT(commandBuffer,logicOp);
}

void graphicsVkCmdSetVertexInputEXT(VkCommandBuffer commandBuffer, uint32_t vertexBindingDescriptionCount,
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, uint32_t vertexAttributeDescriptionCount,
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetVertexInputEXT == nullptr)
    {
        std::cerr << "vkCmdSetVertexInputEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetVertexInputEXT(commandBuffer,vertexBindingDescriptionCount,
    pVertexBindingDescriptions, vertexAttributeDescriptionCount,
    pVertexAttributeDescriptions);
}

void graphicsVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetLogicOpEnableEXT == nullptr)
    {
        std::cerr << "vkCmdSetLogicOpEnableEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetLogicOpEnableEXT(commandBuffer,logicOpEnable);
}

void graphicsVkCmdSetColorBlendEnableEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, uint32_t* pColorBlendEnables)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorBlendEnableEXT == nullptr)
    {
        std::cerr << "vkCmdSetColorBlendEnableEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorBlendEnableEXT(commandBuffer,firstAttachment,attachmentCount,pColorBlendEnables);
}

void graphicsVkCmdSetColorBlendEquationEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, VkColorBlendEquationEXT* pColorBlendEquations)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorBlendEquationEXT == nullptr)
    {
        std::cerr << "vkCmdSetColorBlendEquationEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorBlendEquationEXT(commandBuffer,firstAttachment,attachmentCount,pColorBlendEquations);
}

void graphicsVkCmdSetColorWriteMaskEXT(VkCommandBuffer commandBuffer, uint32_t firstAttachment,
    uint32_t attachmentCount, VkColorComponentFlags* pColorWriteMasks)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorWriteMaskEXT == nullptr)
    {
        std::cerr << "vkCmdSetColorWriteMaskEXT Was Not Loaded" << "\n";
    }
    VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetColorWriteMaskEXT(commandBuffer,firstAttachment,attachmentCount,pColorWriteMasks);
}

VkResult graphicsVkCreateShadersEXT(VkDevice device, uint32_t createInfoCount, VkShaderCreateInfoEXT* pCreateInfos,
    VkAllocationCallbacks* pAllocator, VkShaderEXT* pShaders)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCreateShadersEXT == nullptr)
    {
        std::cerr << "vkCreateShadersEXT Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkCreateShadersEXT(device,createInfoCount,pCreateInfos,pAllocator,pShaders);
}

void graphicsVkDestroyShaderEXT(VkDevice device, VkShaderEXT shader, VkAllocationCallbacks* pAllocator)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkDestroyShaderEXT == nullptr)
    {
        std::cerr << "vkDestroyShaderEXT Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkDestroyShaderEXT(device,shader,pAllocator);
}

void graphicsVkCmdSetRasterizationSamplesEXT(VkCommandBuffer commandBuffer, VkSampleCountFlagBits rasterizationSamples)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetRasterizationSamplesEXT == nullptr)
    {
        std::cerr << "vkCmdSetRasterizationSamplesEXT  Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetRasterizationSamplesEXT(commandBuffer,rasterizationSamples);
}

void graphicsVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetAlphaToCoverageEnableEXT == nullptr)
    {
        std::cerr << "vkCmdSetAlphaToCoverageEnableEXT  Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetAlphaToCoverageEnableEXT(commandBuffer,alphaToCoverageEnable);
}

void graphicsVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetAlphaToOneEnableEXT == nullptr)
    {
        std::cerr << "vkCmdSetAlphaToOneEnableEXT  Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetAlphaToOneEnableEXT(commandBuffer,alphaToOneEnable);
}

void graphicsVkCmdSetSampleMaskEXT(VkCommandBuffer commandBuffer, VkSampleCountFlagBits samples, uint32_t* pSampleMask)
{
    if(VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetSampleMaskEXT == nullptr)
    {
        std::cerr << "vkCmdSetSampleMaskEXT  Was Not Loaded" << "\n";
    }
    
    return VULKAN_HPP_DEFAULT_DISPATCHER.vkCmdSetSampleMaskEXT(commandBuffer,samples,pSampleMask);
}

uintptr_t graphicsCreateSurface(void* instance, GLFWwindow* window)
{
    VkSurfaceKHR surf;

    const auto inst = reinterpret_cast<VkInstance>(instance);

    glfwCreateWindowSurface(inst, window, nullptr, &surf);

    return reinterpret_cast<uintptr_t>(surf);
}
