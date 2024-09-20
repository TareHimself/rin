#include "vulkan/vulkan.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include <iostream>
#include <ranges>

#include "aerox/core/utils.hpp"
#include "aerox/graphics/Allocator.hpp"
#include "aerox/graphics/DeviceBuffer.hpp"
#include "aerox/graphics/DeviceImage.hpp"
#include "aerox/graphics/WindowRenderer.hpp"
#include "aerox/core/GRuntime.hpp"
#include <SDL3/SDL_vulkan.h>
#include "aerox/graphics/ResourceManager.hpp"
#include "aerox/graphics/shaders/ShaderManager.hpp"
#include <VkBootstrap.h>

namespace aerox::graphics
{
    void GraphicsModule::InitVulkan(window::Window* window)
    {
        dispatchLoader.init();

        uint32_t numExtensions = 0;
        
        auto extensions = SDL_Vulkan_GetInstanceExtensions(&numExtensions);
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
        _instance = instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        _debugMessenger = vkbInstance.debug_messenger;
#endif

        vk::PhysicalDeviceShaderObjectFeaturesEXT shaderObjectFeatures{};
        shaderObjectFeatures.setShaderObject(true);

        vk::PhysicalDeviceVulkan13Features features{};
        features.dynamicRendering = true;
        features.synchronization2 = true;

        vk::PhysicalDeviceVulkan12Features features12{};
        features12
        .setBufferDeviceAddress(true)
        .setDescriptorIndexing(true)
        .setDescriptorBindingPartiallyBound(true)
        .setRuntimeDescriptorArray(true)
        .setDescriptorBindingSampledImageUpdateAfterBind(true)
        .setDescriptorBindingStorageImageUpdateAfterBind(true)
        .setDescriptorBindingStorageBufferUpdateAfterBind(true)
        .setDescriptorBindingVariableDescriptorCount(true)
        .setScalarBlockLayout(true);
                  

        auto newRenderer = new WindowRenderer(_instance, window, this);

        _renderers.emplace(window, newRenderer);

        VkSurfaceKHR surf = newRenderer->GetSurface();

        vkb::PhysicalDeviceSelector selector{vkbInstance};

        selector.add_required_extension(vk::EXTShaderObjectExtensionName);

        selector.set_minimum_version(1, 3)
                .set_required_features_13(features)
                .set_required_features_12(features12)
                .set_surface(surf);
        selector.add_required_extension_features(
            static_cast<VkPhysicalDeviceShaderObjectFeaturesEXT>(shaderObjectFeatures));

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

        _device = vkbDevice.device;

        _physicalDevice = physicalDevice.physical_device;

        _queue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

        _queueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).value();

        try
        {
            dispatchLoader.init(_instance);
            dispatchLoader.init(_device);
        }
        catch (std::exception& e)
        {
            std::cerr << "Failed to load vulkan functions " << e.what() << "\n";
            throw;
        }


        _immediateFence = _device.createFence({vk::FenceCreateFlagBits::eSignaled});
        _immediateCommandPool = _device.createCommandPool({vk::CommandPoolCreateFlagBits::eResetCommandBuffer,_queueFamily});
        _immediateCommandBuffer = _device.allocateCommandBuffers({_immediateCommandPool,vk::CommandBufferLevel::ePrimary,1}).at(0);

        _allocator = std::make_unique<Allocator>(this);

        _shaderManager->Init();
        _resourceManager = newShared<ResourceManager>();
        newRenderer->Init();
        onRendererCreated->Invoke(newRenderer);
        
    }

    void GraphicsModule::Startup(GRuntime* runtime)
    {
        _onWindowCreatedHandle = _windowModule->onWindowCreated->Add(this, &GraphicsModule::OnWindowCreated);
        _onWindowDestroyedHandle = _windowModule->onWindowDestroyed->Add(this, &GraphicsModule::OnWindowDestroyed);
        _onTickHandle = runtime->onTick->Add([this](double _)
        {
            for (const auto renderer : _renderers | std::views::values)
            {
                if (renderer->CanDraw())
                {
                    renderer->Draw();
                }
            }
        });
    }

    void GraphicsModule::Shutdown(GRuntime* runtime)
    {
        _device.waitIdle();
        _onTickHandle.UnBind();
        _onWindowCreatedHandle.UnBind();
        _onWindowDestroyedHandle.UnBind();
        for (const auto& renderer : _renderers | std::views::values)
        {
            onRendererDestroyed->Invoke(renderer);
            delete renderer;
        }

        _renderers.clear();

        if (_instance)
        {
            
            _shaderManager->Dispose();
            _resourceManager->Dispose();

            _device.destroyFence(_immediateFence);
            _device.destroyCommandPool(_immediateCommandPool);

            for (auto &[_,item] : _descriptorLayoutStore.GetItems())
            {
                _device.destroyDescriptorSetLayout(item);
            }

            _allocator.reset();
            _device.destroy();
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            vkb::destroy_debug_utils_messenger(_instance, _debugMessenger, nullptr);
#endif
            _instance.destroy();
            
        }
    }

    void GraphicsModule::RegisterRequiredModules()
    {
        Module::RegisterRequiredModules();
        _windowModule = GRuntime::Get()->RegisterModule<window::WindowModule>();
    }

    void GraphicsModule::OnWindowCreated(window::Window* window)
    {
        if (_instance)
        {
            auto renderer = new WindowRenderer(_instance, window, this);
            _renderers.emplace(window, renderer);
            renderer->Init();
            onRendererCreated->Invoke(renderer);
        }
        else
        {
            InitVulkan(window);
        }
    }

    void GraphicsModule::OnWindowDestroyed(window::Window* window)
    {
        if (_renderers.contains(window))
        {
            const auto renderer = _renderers[window];
            _renderers.erase(_renderers.find(window));
            onRendererDestroyed->Invoke(renderer);
            delete renderer;
        }
    }

    GraphicsModule::GraphicsModule()
    {
        _shaderManager = newShared<ShaderManager>(this);
    }

    GraphicsModule* GraphicsModule::Get()
    {
        return GRuntime::Get()->GetModule<GraphicsModule>();
    }

    std::string GraphicsModule::GetName()
    {
        return "Graphics Module";
    }

    bool GraphicsModule::IsDependentOn(Module* module)
    {
        return instanceOf<window::WindowModule>(module);
    }

    vk::DispatchLoaderDynamic GraphicsModule::dispatchLoader = {};

    vk::Instance GraphicsModule::GetInstance() const
    {
        return _instance;
    }

    vk::Device GraphicsModule::GetDevice() const
    {
        return _device;
    }

    vk::PhysicalDevice GraphicsModule::GetPhysicalDevice() const
    {
        return _physicalDevice;
    }

    vk::Queue GraphicsModule::GetQueue() const
    {
        return _queue;
    }

    uint32_t GraphicsModule::GetQueueFamily() const
    {
        return _queueFamily;
    }

    void GraphicsModule::WaitForDeviceIdle() const
    {
        _device.waitIdle();
    }

    ShaderManager* GraphicsModule::GetShaderManager() const
    {
        return _shaderManager.get();
    }

    ResourceManager* GraphicsModule::GetResourceManager() const
    {
        return _resourceManager.get();
    }

    Allocator* GraphicsModule::GetAllocator() const
    {
        return _allocator.get();
    }

    DescriptorLayoutStore* GraphicsModule::GetDescriptorLayoutStore()
    {
        return &_descriptorLayoutStore;
    }

    void GraphicsModule::ImageBarrier(const vk::CommandBuffer cmd, const vk::Image image, const vk::ImageLayout from,
                                      const vk::ImageLayout to,
                                      const ImageBarrierOptions& options)
    {
        vk::ImageMemoryBarrier2 data[] = {
            vk::ImageMemoryBarrier2{
                options.waitForStages,
                options.srcAccessFlags,
                options.nextStages,
                options.dstAccessFlags,
                from,
                to,
                {},
                {},
                image,
                options.subresourceRange
            }
        };
        cmd.pipelineBarrier2({
            {}, {}, {}, data
        });
    }

    void GraphicsModule::ImageBarrier(const vk::CommandBuffer cmd, const Shared<DeviceImage>& image,
                                      const vk::ImageLayout from,
                                      const vk::ImageLayout to, const ImageBarrierOptions& options)
    {
        ImageBarrier(cmd, image->GetImage(), from, to, options);
    }

    void GraphicsModule::ImmediateSubmit(std::function<void(const vk::CommandBuffer&)>&& submit) const
    {
        if(auto result = _device.waitForFences(_immediateFence, true, std::numeric_limits<uint64_t>::max()); result != vk::Result::eSuccess)
        {
            throw std::runtime_error("Failed to wait for fences for immediate submit");
        }

        _device.resetFences(_immediateFence);
        _immediateCommandBuffer.reset();
        _immediateCommandBuffer.begin({vk::CommandBufferUsageFlagBits::eOneTimeSubmit});

        submit(_immediateCommandBuffer);

        _immediateCommandBuffer.end();

        vk::CommandBufferSubmitInfo submits[] = {vk::CommandBufferSubmitInfo{_immediateCommandBuffer}};

        _queue.submit2(vk::SubmitInfo2({}, {}, submits, {}), _immediateFence);

        if(auto result = _device.waitForFences(_immediateFence, true, std::numeric_limits<uint64_t>::max()); result != vk::Result::eSuccess)
        {
            throw std::runtime_error("Failed to wait for fences for immediate submit");
        }
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
                                          const vk::Extent3D& srcExtent,
                                          const vk::Image& dst, const vk::Extent3D& dstExtent, const vk::Filter& filter)
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

    vk::RenderingAttachmentInfo GraphicsModule::MakeRenderingAttachment(const Shared<DeviceImage>& image,
        const vk::ImageLayout& layout, const std::optional<vk::ClearValue>& clearValue)
    {
        return MakeRenderingAttachment(image->GetImageView(),layout,clearValue);
    }

    vk::RenderingAttachmentInfo GraphicsModule::MakeRenderingAttachment(const vk::ImageView& view,
        const vk::ImageLayout& layout, const std::optional<vk::ClearValue>& clearValue)
    {
        vk::RenderingAttachmentInfo attachmentInfo{view,layout};
        attachmentInfo.setLoadOp(clearValue.has_value() ? vk::AttachmentLoadOp::eLoad : vk::AttachmentLoadOp::eClear);
        attachmentInfo.setStoreOp(vk::AttachmentStoreOp::eStore);
        if(clearValue.has_value())
        {
            attachmentInfo.setClearValue(clearValue.value());
        }
        return attachmentInfo;
    }

    vk::ImageCreateInfo GraphicsModule::MakeImageCreateInfo(const vk::Format& format, const vk::Extent3D& extent,
                                                            const vk::ImageUsageFlags usage)
    {
        return vk::ImageCreateInfo{
            {}, vk::ImageType::e2D, format, extent, 1, 1, vk::SampleCountFlagBits::e1, vk::ImageTiling::eOptimal, usage
        };
    }

    vk::ImageViewCreateInfo GraphicsModule::MakeImageViewCreateInfo(const vk::Format& format, const vk::Image& image,
                                                                    const vk::ImageAspectFlags& aspect)
    {
        return vk::ImageViewCreateInfo{
            {}, image, vk::ImageViewType::e2D, format, {},
            vk::ImageSubresourceRange{aspect, 0, vk::RemainingMipLevels, 0, 1}
        };
    }

    vk::ImageViewCreateInfo GraphicsModule::MakeImageViewCreateInfo(const Shared<DeviceImage>& image,
                                                                    const vk::ImageAspectFlags& aspect)
    {
        return MakeImageViewCreateInfo(image->GetFormat(), image->GetImage(), aspect);
    }

    void GraphicsModule::GenerateMipMaps(const vk::CommandBuffer& cmd, const Shared<DeviceImage>& image,
                                         const vk::Extent3D& extent, const vk::Filter& filter)
    {
        auto mipLevels = DeriveMipLevels(extent);
        auto curSize = extent;
        for (auto mip = 0; mip < mipLevels; mip++)
        {
            auto halfSize = curSize;
            halfSize.width /= 2;
            halfSize.height /= 2;
            ImageBarrier(cmd, image, vk::ImageLayout::eTransferDstOptimal, vk::ImageLayout::eTransferSrcOptimal,
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
                    {},
                    vk::ImageSubresourceLayers{
                        vk::ImageAspectFlagBits::eColor,
                        static_cast<uint32_t>(mip + 1),
                        0,
                        1
                    },
                    {}
                };


                blitRegion.srcOffsets[1] = vk::Offset3D{
                    static_cast<int>(curSize.width), static_cast<int>(curSize.height), 1
                };
                blitRegion.dstOffsets[1] = vk::Offset3D{
                    static_cast<int>(halfSize.width), static_cast<int>(halfSize.height), 1
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
            }
        }

        ImageBarrier(cmd, image, vk::ImageLayout::eTransferSrcOptimal,
                     vk::ImageLayout::eShaderReadOnlyOptimal);
    }

    WindowRenderer* GraphicsModule::GetRenderer(window::Window* window) const
    {
        if(_renderers.contains(window))
        {
            return _renderers.at(window);
        }

        return nullptr;
    }

    Shared<DeviceImage> GraphicsModule::CreateImage(const vk::Extent3D& extent, const vk::Format format,
                                                    const vk::ImageUsageFlags usage,
                                                    const bool mipMap, const std::string& debugName) const
    {
        auto imageInfo = MakeImageCreateInfo(format, extent, usage);

        if (mipMap)
        {
            imageInfo.setMipLevels(DeriveMipLevels(extent));
        }

        auto newImage = _allocator->NewImage(imageInfo, debugName);

        auto aspectFlags = vk::ImageAspectFlagBits::eColor;
        if (format == vk::Format::eD32Sfloat) aspectFlags = vk::ImageAspectFlagBits::eDepth;

        auto viewCreateInfo = MakeImageViewCreateInfo(newImage, aspectFlags);

        viewCreateInfo.subresourceRange.levelCount = imageInfo.mipLevels;

        _device.createImageView(&viewCreateInfo, nullptr, newImage->GetImageViewRef());

        return newImage;
    }

    Shared<DeviceImage> GraphicsModule::CreateImage(const std::vector<std::byte>& data, const vk::Extent3D& extent,
                                                    vk::Format format, vk::ImageUsageFlags usage, bool mipMap,
                                                    const vk::Filter& mipMapFilter,
                                                    const std::string& debugName) const
    {
        auto dataSize = extent.depth * extent.width * extent.height * 4;

        auto uploadBuffer = _allocator->NewTransferBuffer(dataSize);

        uploadBuffer->Write(data);

        auto newImage = CreateImage(extent, format,
                                    usage | vk::ImageUsageFlagBits::eTransferSrc | vk::ImageUsageFlagBits::eTransferDst,
                                    mipMap, debugName);

        ImmediateSubmit([newImage, extent, &uploadBuffer, mipMap, mipMapFilter](const vk::CommandBuffer& cmd)
        {
            ImageBarrier(cmd, newImage->GetImage(), vk::ImageLayout::eUndefined, vk::ImageLayout::eTransferDstOptimal);

            auto copyRegion = vk::BufferImageCopy{
                0,
                0,
                0,
                vk::ImageSubresourceLayers{
                    vk::ImageAspectFlagBits::eColor,
                    0,
                    0,
                    1
                },
                {},
                extent
            };

            cmd.copyBufferToImage(uploadBuffer->GetBuffer(), newImage->GetImage(), vk::ImageLayout::eTransferDstOptimal,
                                  copyRegion);

            if (mipMap)
            {
                GenerateMipMaps(cmd, newImage, extent, mipMapFilter);
            }
            else
            {
                ImageBarrier(cmd, newImage->GetImage(), vk::ImageLayout::eTransferDstOptimal,
                             vk::ImageLayout::eShaderReadOnlyOptimal);
            }
        });

        return newImage;
    }
}
