#define VMA_IMPLEMENTATION
//#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "graphics.hpp"
#include "rwin/rwin.h"
VULKAN_HPP_DEFAULT_DISPATCH_LOADER_DYNAMIC_STORAGE
#include <vulkan/vulkan.hpp>
#include <VkBootstrap.h>
#include <iostream>
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

GraphicsResult createVulkanInstance(std::uint64_t windowHandle, VulkanInitResult* outResult)
{
    return guarded([&]
    {
        require(outResult != nullptr, GraphicsResult::InvalidArgument);

        VULKAN_HPP_DEFAULT_DISPATCHER.init();

        vkb::InstanceBuilder builder{};

        std::vector<const char*> requiredExtensions{};
        rwin::getRequiredExtensions(requiredExtensions);
        builder
            .set_app_name("Rin Engine")
            .require_api_version(1,3,0)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()
#endif
            .enable_extensions(requiredExtensions);

        auto instanceResult = builder.build();

        if(!instanceResult)
        {
            std::cerr << "Failed to create Vulkan instance: " << instanceResult.error().message() << "\n";
            throw ResultError(GraphicsResult::InstanceCreationFailed);
        }

        auto vkbInstance = instanceResult.value();

        auto instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        outResult->messenger = vkbInstance.debug_messenger;
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
            .setShaderSampledImageArrayNonUniformIndexing(true)
            .setDescriptorBindingSampledImageUpdateAfterBind(true)
            .setDescriptorBindingStorageImageUpdateAfterBind(true)
            .setDescriptorBindingStorageBufferUpdateAfterBind(true)
            .setDescriptorBindingVariableDescriptorCount(true)
            .setScalarBlockLayout(true);

        VkSurfaceKHR surf = rwin::createSurface(windowHandle,instance);
        require(surf != VK_NULL_HANDLE, GraphicsResult::SurfaceCreationFailed);

        vkb::PhysicalDeviceSelector selector{vkbInstance};
        selector.set_minimum_version(1,3)
                .set_required_features_13(features)
                .set_required_features_12(features12)
                .set_surface(surf);
        selector
        .add_required_extension_features(static_cast<VkPhysicalDeviceShaderDrawParametersFeatures>(drawParametersFeatures));

        auto physicalDevicesResult = selector.select_devices();

        if(!physicalDevicesResult)
        {
            std::cerr << "Failed to select vulkan physical device: " << physicalDevicesResult.error().message() << "\n";
            throw ResultError(GraphicsResult::NoSuitableDevice);
        }

        VkPhysicalDeviceFeatures indirectFeatures{};
        indirectFeatures.multiDrawIndirect = VK_TRUE;
        // The engine writes indirect commands with a non-zero firstInstance, which needs this feature.
        indirectFeatures.drawIndirectFirstInstance = VK_TRUE;
        VkPhysicalDeviceVulkan12Features indirectFeatures12{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES};
        indirectFeatures12.drawIndirectCount = VK_TRUE;

        // Candidates are copies so a device with only some of the features isn't left half-enabled.
        auto physicalDevice = physicalDevicesResult.value().front();
        outResult->supportsIndirectRendering = false;
        for(auto candidate : physicalDevicesResult.value())
        {
            if(candidate.enable_features_if_present(indirectFeatures) &&
               candidate.enable_extension_features_if_present(indirectFeatures12))
            {
                physicalDevice = candidate;
                outResult->supportsIndirectRendering = true;
                break;
            }
        }
        vkb::DeviceBuilder deviceBuilder{physicalDevice};

        auto deviceResult = deviceBuilder.build();

        if(!deviceResult)
        {
            std::cerr << "Failed to build vulkan device: " << deviceResult.error().message() << "\n";
            throw ResultError(GraphicsResult::DeviceCreationFailed);
        }

        const vkb::Device& vkbDevice = deviceResult.value();

        auto device = vkbDevice.device;

        auto gpu = physicalDevice.physical_device;

        const auto graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics);
        const auto graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics);
        require(graphicsQueue.has_value() && graphicsQueueFamily.has_value(), GraphicsResult::NoSuitableDevice);

        outResult->graphicsQueue = graphicsQueue.value();
        outResult->graphicsQueueFamily = graphicsQueueFamily.value();
        // A transfer or compute queue is only worth having when it is not the graphics family, otherwise both
        // fall back to the graphics queue and the caller sees one family everywhere.
        const auto transfer = vkbDevice.get_queue(vkb::QueueType::transfer);
        const auto transferFamily = vkbDevice.get_queue_index(vkb::QueueType::transfer);
        const auto hasTransferQueue = transfer.has_value() && transferFamily.has_value() &&
                                      transferFamily.value() != graphicsQueueFamily.value();
        outResult->transferQueue = hasTransferQueue ? transfer.value() : graphicsQueue.value();
        outResult->transferQueueFamily = hasTransferQueue ? transferFamily.value() : graphicsQueueFamily.value();
        // get_dedicated_queue_index would also reject TRANSFER, which NVIDIA and AMD set on their async compute
        // family, so it would report false there.
        const auto compute = vkbDevice.get_queue(vkb::QueueType::compute);
        const auto computeFamily = vkbDevice.get_queue_index(vkb::QueueType::compute);
        const auto hasComputeQueue = compute.has_value() && computeFamily.has_value() &&
                                     computeFamily.value() != graphicsQueueFamily.value();
        outResult->computeQueue = hasComputeQueue ? compute.value() : graphicsQueue.value();
        outResult->computeQueueFamily = hasComputeQueue ? computeFamily.value() : graphicsQueueFamily.value();
        outResult->supportsDedicatedCompute = hasComputeQueue;

        try
        {
            VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Instance(instance));
            VULKAN_HPP_DEFAULT_DISPATCHER.init(vk::Device(device));
        }
        catch(const std::exception& e)
        {
            std::cerr << "Failed to load vulkan functions " << e.what() << "\n";
            throw ResultError(GraphicsResult::FunctionLoadingFailed);
        }

        outResult->instance = instance;
        outResult->device = device;
        outResult->physicalDevice = gpu;
        outResult->surface = surf;
    });
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
    VmaAllocationCreateInfo vmaAllocInfo = {};
    vmaAllocInfo.flags = flags;
    vmaAllocInfo.usage = memoryUsage;
    vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);

    const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;

    check(vmaCreateBuffer(allocator,&vmaBufferCreateInfo,&vmaAllocInfo,buffer,allocation,
                    nullptr));
    vmaSetAllocationName(allocator,*allocation,name);
}

GraphicsResult allocatorCreate(VkInstance instance, VkDevice device, VkPhysicalDevice physicalDevice, void** outAllocator)
{
    return guarded([&]
    {
        require(outAllocator != nullptr, GraphicsResult::InvalidArgument);
        auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
        allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
        allocatorCreateInfo.device = (device);
        allocatorCreateInfo.physicalDevice = (physicalDevice);
        allocatorCreateInfo.instance = (instance);
        allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0,1,3,0);
        VmaAllocator allocator;
        check(vmaCreateAllocator(&allocatorCreateInfo,&allocator));
        *outAllocator = allocator;
    });
}

void allocatorDestroy(void* allocator)
{
    vmaDestroyAllocator(static_cast<VmaAllocator>(allocator));
}

GraphicsResult allocatorNewBuffer(VkBuffer* buffer, void** allocation, size_t size, void* allocator,
    int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
    int mapped, const char* debugName)
{
    return guarded([&]
    {
        require(buffer != nullptr && allocation != nullptr && allocator != nullptr, GraphicsResult::InvalidArgument);
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
    });
}

GraphicsResult allocatorNewImage(VkImage* image, void** allocation, VkImageCreateInfo* createInfo, void* allocator,
    const char* debugName)
{
    return guarded([&]
    {
        require(image != nullptr && allocation != nullptr && createInfo != nullptr && allocator != nullptr,
                GraphicsResult::InvalidArgument);
        VmaAllocationCreateInfo imageAllocInfo = {};

        imageAllocInfo.usage = VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
        imageAllocInfo.requiredFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
        const auto actualAllocator = static_cast<VmaAllocator>(allocator);

        VmaAllocation alloc;
        check(vmaCreateImage(actualAllocator,createInfo,&imageAllocInfo,image,&alloc,
                                     nullptr));

        vmaSetAllocationName(actualAllocator,alloc,debugName);
        *allocation = static_cast<void*>(alloc);
    });
}

void allocatorSetAllocationName(void* allocator, void* allocation, const char* name)
{
    vmaSetAllocationName(static_cast<VmaAllocator>(allocator), static_cast<VmaAllocation>(allocation), name);
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

GraphicsResult allocatorCopyToBuffer(void* allocator, void* allocation, void* data, const size_t size,
    size_t offset)
{
    return guarded([&]
    {
        require(allocator != nullptr && allocation != nullptr && (data != nullptr || size == 0),
                GraphicsResult::InvalidArgument);
        check(vmaCopyMemoryToAllocation(static_cast<VmaAllocator>(allocator),data,static_cast<VmaAllocation>(allocation),
                                  offset,size));
    });
}

void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, uint32_t stageCount, VkShaderStageFlagBits* pStages,
    VkShaderEXT* pShaders)
{
    VK_DISPATCH_CHECKED(vkCmdBindShadersEXT,commandBuffer,stageCount,pStages,pShaders)
}

