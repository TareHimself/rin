#define VMA_IMPLEMENTATION
//#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "graphics.hpp"
#include "rwin/rwin.h"
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

void createVulkanInstance(std::uint64_t windowHandle, VkInstance* outInstance, VkDevice* outDevice,
                          VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily, VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily,
                          VkSurfaceKHR* outSurface,
                          VkDebugUtilsMessengerEXT* outMessenger)
{

    VULKAN_HPP_DEFAULT_DISPATCHER.init();

    auto systemInfo = vkb::SystemInfo::get_system_info().value();


    vkb::InstanceBuilder builder{};


    std::vector<const char*> requiredExtensions{};
    rwin::getRequiredExtensions(requiredExtensions);
    builder
        .set_app_name("Rin Engine")
        .require_api_version(1,3,0)
        //.request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        .use_default_debug_messenger()
#endif
        .enable_extensions(requiredExtensions);

    VkPipelineStageFlagBits2 pipelineStageFlagBits2{};
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

    VkSurfaceKHR surf = rwin::createSurface(windowHandle,instance);
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

void destroyVulkanMessenger(VkInstance instance, VkDebugUtilsMessengerEXT messenger)
{
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    vkb::destroy_debug_utils_messenger(instance,messenger);
    //inst.destroyDebugUtilsMessengerEXT(messengerCasted);
#endif
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

void* allocatorCreate(VkInstance instance, VkDevice device, VkPhysicalDevice physicalDevice)
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

void allocatorDestroy(void* allocator)
{
    vmaDestroyAllocator(static_cast<VmaAllocator>(allocator));
}

void allocatorNewBuffer(VkBuffer* buffer, void** allocation, size_t size, void* allocator,
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

void allocatorNewImage(VkImage* image, void** allocation, VkImageCreateInfo* createInfo, void* allocator,
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

void allocatorFreeBuffer(VkBuffer buffer, void* allocation, void* allocator)
{
    vmaDestroyBuffer(static_cast<VmaAllocator>(allocator),buffer,
                     static_cast<VmaAllocation>(allocation));
}

void allocatorFreeImage(VkImage image, void* allocation, void* allocator)
{
    vmaDestroyImage(static_cast<VmaAllocator>(allocator),image,
                    static_cast<VmaAllocation>(allocation));
}

void allocatorCopyToBuffer(void* allocator, void* allocation, void* data, const size_t size,
    size_t offset)
{
    // auto dataCasted = static_cast<TestStruct*>(data);
    // std::cout << "SENDING TO SHADER " << dataCasted->viewport[2] << std::endl;
    vmaCopyMemoryToAllocation(static_cast<VmaAllocator>(allocator),data,static_cast<VmaAllocation>(allocation),
                              offset,size);
}

void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint32_t stageCount, VkShaderStageFlagBits* pStages,
    VkShaderEXT* pShaders)
{
    VK_DISPATCH_CHECKED(vkCmdBindShadersEXT,commandBuffer,stageCount,pStages,pShaders)
}

