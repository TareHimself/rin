#define VMA_IMPLEMENTATION
#include <vk_mem_alloc.h>
#include "rin/graphics/GraphicsModule.h"
#include "VkBootstrap.h"
#include "rin/graphics/DefaultTextureManager.h"
#include "rin/graphics/DeviceBuffer.h"
#include "rin/graphics/DeviceImage.h"
#include "rin/graphics/slang/SlangShaderManager.h"
VULKAN_HPP_DEFAULT_DISPATCH_LOADER_DYNAMIC_STORAGE

#include "rin/core/GRuntime.h"

namespace rin::graphics
{
    void GraphicsModule::OnDispose()
    {
        Module::OnDispose();
    }

    void GraphicsModule::Startup(GRuntime* runtime)
    {
        VULKAN_HPP_DEFAULT_DISPATCHER.init();

        auto window = _ioModule->CreateWindow({1, 1}, "", io::IWindow::CreateOptions().Visible(false));
        auto extensions = window->GetRequiredExtensions();

        auto systemInfo = vkb::SystemInfo::get_system_info().value();


        vkb::InstanceBuilder builder{};


        builder
            .set_app_name("Aerox")
            .require_api_version(1, 3, 0)

            //.request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()

#endif
            ;
        for (auto& extension : extensions)
        {
            builder.enable_extension(extension.c_str());
        }

        builder.enable_layer("VK_LAYER_KHRONOS_shader_object");

        if (systemInfo.is_extension_available(vk::EXTShaderObjectExtensionName))
        {
            builder.enable_extension(vk::EXTShaderObjectExtensionName);
        }


        auto instanceResult = builder.build();

        if (!instanceResult)
        {
            throw std::runtime_error(instanceResult.error().message());
        }

        auto vkbInstance = instanceResult.value();

        _instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        _debugUtilsMessengerExt = vkbInstance.debug_messenger;
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
                  .setScalarBlockLayout(true)
                  .setBufferDeviceAddress(true);


        auto surface = window->CreateSurface(_instance);

        vkb::PhysicalDeviceSelector selector{vkbInstance};

        selector.add_required_extension(vk::EXTShaderObjectExtensionName);

        selector.set_minimum_version(1, 3)
                .set_required_features_13(features)
                .set_required_features_12(features12)
                .set_surface(surface);
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
            throw std::runtime_error(physicalDeviceResult.error().message());
        }

        vkb::PhysicalDevice physicalDevice = physicalDeviceResult.value();

        physicalDevice.enable_extension_if_present(vk::EXTShaderObjectExtensionName);
        vkb::DeviceBuilder deviceBuilder{physicalDevice};

        auto deviceResult = deviceBuilder.build();

        if (!deviceResult)
        {
            throw std::runtime_error(deviceResult.error().message());
        }

        vkb::Device vkbDevice = deviceResult.value();

        _device = vkbDevice.device;

        _physicalDevice = physicalDevice.physical_device;

        _graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();
        auto transfer = vkbDevice.get_queue(vkb::QueueType::transfer);
        // std::cout << "INit VULKAN " << std::endl;
        // std::cout << "Transfer QUeue Error " << transfer.error().message() << std::endl;
        auto hasTransferQueue = transfer.has_value();
        _transferQueue = hasTransferQueue ? transfer.value() : vkbDevice.get_queue(vkb::QueueType::graphics).value();

        _graphicsQueueIndex = vkbDevice.get_queue_index(vkb::QueueType::graphics).value();
        _transferQueueIndex = hasTransferQueue
                                  ? vkbDevice.get_queue_index(vkb::QueueType::transfer).value()
                                  : vkbDevice.get_queue_index(vkb::QueueType::graphics).value();

        VULKAN_HPP_DEFAULT_DISPATCHER.init(_instance);
        VULKAN_HPP_DEFAULT_DISPATCHER.init(_device);

        window->Dispose();

        _immediateFence = _device.createFence({vk::FenceCreateFlagBits::eSignaled});
        _immediateCommandPool = _device.createCommandPool(
        {vk::CommandPoolCreateFlagBits::eResetCommandBuffer, _transferQueueIndex});
        _immediateCommandBuffer = _device.allocateCommandBuffers({
            _immediateCommandPool, vk::CommandBufferLevel::ePrimary, 1
        }).at(0);

        auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
        allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
        allocatorCreateInfo.device = _device;
        allocatorCreateInfo.physicalDevice = _physicalDevice;
        allocatorCreateInfo.instance = _instance;
        allocatorCreateInfo.vulkanApiVersion = VK_MAKE_VERSION(1, 3, 0);
        vmaCreateAllocator(&allocatorCreateInfo, &_allocator);

        _textureManager = shared<DefaultTextureManager>();
        _shaderManager = shared<SlangShaderManager>();
    }

    void GraphicsModule::Shutdown(GRuntime* runtime)
    {
        _shaderManager->Dispose();
        _textureManager->Dispose();
        vmaDestroyAllocator(_allocator);
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        vkb::destroy_debug_utils_messenger(_instance, _debugUtilsMessengerExt);
        //inst.destroyDebugUtilsMessengerEXT(messengerCasted);
#endif

        _instance.destroy();
    }

    std::string GraphicsModule::GetName()
    {
        return "graphics";
    }

    Shared<IDeviceImage> GraphicsModule::NewImage(const vk::Extent3D& extent, const ImageFormat& format,
                                                  const vk::ImageUsageFlags& usage, bool mips,
                                                  const std::string& debugName)
    {
        auto imageCreateInfo = MakeImageCreateInfo(format, extent, usage);

        if (mips)
        {
            imageCreateInfo.setMipLevels(DeriveMipLevels(extent));
        }

        VmaAllocationCreateInfo imageAllocInfo = {};

        imageAllocInfo.usage = VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
        imageAllocInfo.requiredFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
        const auto vmaImageCreateInfo = static_cast<VkImageCreateInfo>(imageCreateInfo);

        VkImage vmaImage;
        VmaAllocation alloc;
        auto result = vmaCreateImage(_allocator, &vmaImageCreateInfo, &imageAllocInfo, &vmaImage, &alloc,
                                     nullptr);

        if (result != VK_SUCCESS)
        {
            throw std::runtime_error("Failed to create image");
        }

        vmaSetAllocationName(_allocator, alloc, debugName.c_str());
        
        
        auto aspectFlags = vk::ImageAspectFlagBits::eColor;
        if (format == ImageFormat::Depth) aspectFlags = vk::ImageAspectFlagBits::eDepth;
        if (format == ImageFormat::Stencil) aspectFlags = vk::ImageAspectFlagBits::eStencil;

        auto viewCreateInfo = MakeImageViewCreateInfo(format,vmaImage, aspectFlags);

        viewCreateInfo.subresourceRange.levelCount = imageCreateInfo.mipLevels;

        vk::ImageView view;
        if (auto viewCreateResult = _device.createImageView(&viewCreateInfo, nullptr, &view); viewCreateResult !=
            vk::Result::eSuccess)
        {
            throw std::runtime_error("Failed to create image view: " + to_string(viewCreateResult));
        }

        return shared<DeviceImage>(_allocator,alloc,vmaImage,view,extent,format);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewBuffer(uint64_t size, const vk::BufferUsageFlags& usage,
                                                    const vk::MemoryPropertyFlags& properties, bool sequentialWrite,
                                                    bool preferHost, bool mapped,
                                                    const std::string& debugName)
    {
        vk::BufferCreateInfo createInfo{{}, size, usage};

        VmaAllocationCreateFlags vmaFlags = sequentialWrite
                                                ? VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT
                                                : VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT;
        if (mapped)
        {
            vmaFlags |= VMA_ALLOCATION_CREATE_MAPPED_BIT;
        }

        VmaAllocationCreateInfo allocInfo{};
        allocInfo.flags = vmaFlags;
        allocInfo.usage = preferHost ? VMA_MEMORY_USAGE_AUTO_PREFER_HOST : VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
        allocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(properties);
        allocInfo.preferredFlags = 0;
        VkBufferCreateInfo vkCreateInfo = createInfo;
        VkBuffer vkBuffer;
        VmaAllocation allocation;
        vmaCreateBuffer(_allocator, &vkCreateInfo, &allocInfo, &vkBuffer, &allocation, nullptr);
        vmaSetAllocationName(_allocator, allocation, debugName.c_str());
        return shared<DeviceBuffer>(_allocator, allocation, vkBuffer, size);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewTransferBuffer(uint64_t size, bool sequentialWrite,
                                                            const std::string& debugName)
    {
        return NewBuffer(size, vk::BufferUsageFlagBits::eTransferSrc,
                         vk::MemoryPropertyFlagBits::eHostVisible | vk::MemoryPropertyFlagBits::eHostCoherent,
                         sequentialWrite, true, true, debugName);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewStorageBuffer(uint64_t size, bool sequentialWrite,
                                                           const std::string& debugName)
    {
        return NewBuffer(size, vk::BufferUsageFlagBits::eStorageBuffer | vk::BufferUsageFlagBits::eShaderDeviceAddress,
                         vk::MemoryPropertyFlagBits::eHostVisible, sequentialWrite, false, true, debugName);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewUniformBuffer(uint64_t size, bool sequentialWrite,
                                                           const std::string& debugName)
    {
        return NewBuffer(size, vk::BufferUsageFlagBits::eUniformBuffer, vk::MemoryPropertyFlagBits::eHostVisible,
                         sequentialWrite, false, true, debugName);
    }

    ITextureManager* GraphicsModule::GetTextureManager() const
    {
        return _textureManager.get();
    }

    IShaderManager* GraphicsModule::GetShaderManager() const
    {
        return _shaderManager.get();
    }

    uint32_t GraphicsModule::DeriveMipLevels(const vk::Extent2D& extent)
    {
        return (1 + static_cast<uint32_t>(std::floor(std::log2(std::max(extent.width, extent.height)))));
    }

    uint32_t GraphicsModule::DeriveMipLevels(const vk::Extent3D& extent)
    {
        return DeriveMipLevels(vk::Extent2D{extent.width, extent.height});
    }

    void GraphicsModule::CopyImageToImage(const vk::CommandBuffer& cmd, const vk::Image& src,
                                          const vk::Extent3D& srcExtent, const vk::Image& dst,
                                          const vk::Extent3D& dstExtent, const vk::Filter& filter)
    {
        auto blitRegion = vk::ImageBlit2{
            vk::ImageSubresourceLayers{vk::ImageAspectFlagBits::eColor, 0, 0, 1}, {},
            vk::ImageSubresourceLayers{vk::ImageAspectFlagBits::eColor, 0, 0, 1}, {}
        };
        blitRegion.srcOffsets[1] = vk::Offset3D{
            static_cast<int>(srcExtent.width), static_cast<int>(srcExtent.height), static_cast<int>(srcExtent.depth)
        };
        blitRegion.dstOffsets[1] = vk::Offset3D{
            static_cast<int>(dstExtent.width), static_cast<int>(dstExtent.height), static_cast<int>(dstExtent.depth)
        };
        const auto blitInfo = vk::BlitImageInfo2{
            src, vk::ImageLayout::eTransferSrcOptimal, dst, vk::ImageLayout::eTransferDstOptimal, blitRegion, filter
        };

        cmd.blitImage2(blitInfo);
    }

    vk::RenderingAttachmentInfo GraphicsModule::MakeRenderingAttachment(const Shared<IDeviceImage>& image,
                                                                        const vk::ImageLayout& layout,
                                                                        const std::optional<vk::ClearValue>& clearValue)
    {
        return MakeRenderingAttachment(image->GetImageView(), layout, clearValue);
    }

    vk::RenderingAttachmentInfo GraphicsModule::MakeRenderingAttachment(const vk::ImageView& view,
                                                                        const vk::ImageLayout& layout,
                                                                        const std::optional<vk::ClearValue>& clearValue)
    {
        vk::RenderingAttachmentInfo attachmentInfo{view, layout};
        attachmentInfo.setLoadOp(clearValue.has_value() ? vk::AttachmentLoadOp::eClear : vk::AttachmentLoadOp::eLoad);
        attachmentInfo.setStoreOp(vk::AttachmentStoreOp::eStore);
        if (clearValue.has_value())
        {
            attachmentInfo.setClearValue(clearValue.value());
        }
        return attachmentInfo;
    }

    vk::ImageCreateInfo GraphicsModule::MakeImageCreateInfo(ImageFormat format, const vk::Extent3D& extent,
                                                            vk::ImageUsageFlags usage)
    {
        return vk::ImageCreateInfo{
            {}, vk::ImageType::e2D, imageFormatToVulkanFormat(format), extent, 1, 1, vk::SampleCountFlagBits::e1,
            vk::ImageTiling::eOptimal, usage
        };
    }

    vk::ImageViewCreateInfo GraphicsModule::MakeImageViewCreateInfo(ImageFormat format, const vk::Image& image,
                                                                    const vk::ImageAspectFlags& aspect)
    {
        return vk::ImageViewCreateInfo{
            {}, image, vk::ImageViewType::e2D, imageFormatToVulkanFormat(format), {},
            vk::ImageSubresourceRange{aspect, 0, vk::RemainingMipLevels, 0, 1}
        };
    }

    vk::ImageViewCreateInfo GraphicsModule::MakeImageViewCreateInfo(const Shared<IDeviceImage>& image,
                                                                    const vk::ImageAspectFlags& aspect)
    {
        return MakeImageViewCreateInfo(image->GetFormat(), image->GetImage(), aspect);
    }

    void GraphicsModule::GenerateMipMaps(const vk::CommandBuffer& cmd, const Shared<IDeviceImage>& image,
                                         const vk::Extent3D& extent, const vk::Filter& filter)
    {
        auto mipLevels = DeriveMipLevels(extent);
        auto curSize = extent;
        for (int mip = 0; mip < mipLevels; mip++)
        {
            auto halfSize = curSize;
            halfSize.width /= 2;
            halfSize.height /= 2;
            image->Barrier(cmd, vk::ImageLayout::eTransferDstOptimal, vk::ImageLayout::eTransferSrcOptimal,
                           ImageBarrierOptions().SubResource({
                               vk::ImageAspectFlagBits::eColor,
                               static_cast<uint32_t>(mip),
                               1,
                               0,
                               vk::RemainingArrayLayers
                           }));
            if (mip < mipLevels - 1)
            {
                auto blitRegion = vk::ImageBlit2{
                    vk::ImageSubresourceLayers{
                        vk::ImageAspectFlagBits::eColor,
                        static_cast<uint32_t>(mip),
                        0,
                        1
                    },
                    {
                        vk::Offset3D{}, vk::Offset3D{
                            static_cast<int>(curSize.width), static_cast<int>(curSize.height), 1
                        }
                    },
                    vk::ImageSubresourceLayers{
                        vk::ImageAspectFlagBits::eColor,
                        static_cast<uint32_t>(mip + 1),
                        0,
                        1
                    },
                    {
                        vk::Offset3D{}, vk::Offset3D{
                            static_cast<int>(halfSize.width), static_cast<int>(halfSize.height), 1
                        }
                    }
                };

                auto blitInfo = vk::BlitImageInfo2{
                    image->GetImage(),
                    vk::ImageLayout::eTransferSrcOptimal,
                    image->GetImage(),
                    vk::ImageLayout::eTransferDstOptimal,
                    blitRegion,
                    filter
                };

                cmd.blitImage2(blitInfo);
                curSize = halfSize;
            }
        }

        image->Barrier(cmd, vk::ImageLayout::eTransferSrcOptimal,
                       vk::ImageLayout::eShaderReadOnlyOptimal);
    }

    vk::DispatchLoaderDynamic GraphicsModule::GetDispatchLoader()
    {
        return VULKAN_HPP_DEFAULT_DISPATCHER;
    }

    void GraphicsModule::RegisterRequiredModules(GRuntime* runtime)
    {
        _ioModule = runtime->RegisterModule<io::IoModule>();
    }

    bool GraphicsModule::IsDependentOn(Module* module)
    {
        return module == _ioModule;
    }
}
