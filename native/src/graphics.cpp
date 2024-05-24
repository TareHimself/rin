#define VMA_IMPLEMENTATION
#include "graphics.hpp"

#include <iostream>
#include <vulkan/vulkan.hpp>
#include <VkBootstrap.h>
#include <spirv_glsl.hpp>


void graphicsCreateVulkanInstance(void* inWindow, void** outInstance, void** outDevice, void** outPhysicalDevice,
                                  void** outQueue, uint32_t* outQueueFamily, uintptr_t* outSurface,
                                  uintptr_t* outMessenger)
{
    uint32_t numExtensions = 0;
    auto extensions = glfwGetRequiredInstanceExtensions(&numExtensions);

    vkb::InstanceBuilder builder;
    auto instanceResult =
        builder.set_app_name("Aerox")
               .require_api_version(1, 3, 0)
               //.request_validation_layers(true)
               .enable_extensions(numExtensions, extensions)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        .use_default_debug_messenger()
#endif

        .build();

    auto vkbInstance = instanceResult.value();
    auto instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
    *outMessenger = reinterpret_cast<uintptr_t>(vkbInstance.debug_messenger);
#endif

    vk::PhysicalDeviceVulkan13Features features;
    features.dynamicRendering = true;
    features.synchronization2 = true;

    vk::PhysicalDeviceVulkan12Features features12;
    features12.setBufferDeviceAddress(true)
              .setDescriptorIndexing(true)
              .setDescriptorBindingSampledImageUpdateAfterBind(true)
              .setDescriptorBindingStorageImageUpdateAfterBind(true)
              .setScalarBlockLayout(true)
              .setDescriptorBindingUniformBufferUpdateAfterBind(true);

    VkSurfaceKHR surf;
    glfwCreateWindowSurface(instance, static_cast<GLFWwindow*>(inWindow), nullptr, &surf);
    vkb::PhysicalDeviceSelector selector{vkbInstance};
    vkb::PhysicalDevice physicalDevice =
        selector.set_minimum_version(1, 3)
                .set_required_features_13(features)
                .set_required_features_12(features12)
                .set_surface(surf)
                .select()
                .value();

    vkb::DeviceBuilder deviceBuilder{physicalDevice};

    vkb::Device vkbDevice = deviceBuilder.build().value();

    auto device = vkbDevice.device;

    auto gpu = physicalDevice.physical_device;

    auto graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

    auto graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).
                                         value();


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

void graphicsReflectShader(void* inShader, uint32_t shaderSize, const ShaderReflectedCallback reflectedCallback)
{
    auto spvData = std::vector<uint32_t>();
    spvData.resize(shaderSize / sizeof(uint32_t));
    memcpy(spvData.data(), inShader, shaderSize);

    try
    {
        const spirv_cross::CompilerGLSL glsl(spvData);

        spirv_cross::ShaderResources resources = glsl.get_shader_resources();

        auto images = std::vector<json>();
        auto pushConstants = std::vector<json>();
        auto buffers = std::vector<json>();

        for (auto& resource : resources.sampled_images)
        {
            unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
            unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);
            auto numArray = glsl.get_type(resource.type_id).array;
            auto numRequired = std::max(numArray.empty() ? 0 : numArray[0], static_cast<uint32_t>(1));
            images.push_back(
                json({{"name", resource.name}, {"set", set}, {"binding", binding}, {"count", numRequired}}));
        }

        for (auto& resource : resources.push_constant_buffers)
        {
            uint32_t size;
            size = glsl.get_declared_struct_size(glsl.get_type(resource.base_type_id));
            uint32_t offset = 0; // Needs work glsl.get_decoration(resource.id,spv::DecorationOffset);
            pushConstants.push_back(json({{"name", resource.name}, {"size", size}, {"offset", offset}}));
        }

        for (auto& resource : resources.uniform_buffers)
        {
            unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
            unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);

            auto numArray = glsl.get_type(resource.type_id).array;
            auto numRequired = std::max(numArray.empty() ? 0 : numArray[0], static_cast<uint32_t>(1));
            buffers.push_back(json({
                {"name", glsl.get_name(resource.id)}, {"set", set}, {"binding", binding}, {"count", numRequired}
            }));
        }

        const json jsonResult = {{"images", images}, {"pushConstants", pushConstants}, {"buffers", buffers}};

        std::string strResult = jsonResult.dump();

        reflectedCallback(strResult.data(), strResult.size());
    }
    catch (std::exception& e)
    {
        std::cerr << e.what() << std::endl;
    }
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
                                               ? VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT : VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT;
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

// struct TestStruct
// {
//     float time;
//     float viewport[4];
//     
// };

void graphicsAllocatorCopyToBuffer(void* allocator, void* allocation, void* data, const unsigned long size,
                                   unsigned long offset)
{
    // auto dataCasted = static_cast<TestStruct*>(data);
    // std::cout << "SENDING TO SHADER " << dataCasted->viewport[2] << std::endl;
    vmaCopyMemoryToAllocation(static_cast<VmaAllocator>(allocator), data, static_cast<VmaAllocation>(allocation),
                              offset, size);
}
