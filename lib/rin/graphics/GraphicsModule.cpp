#define VMA_IMPLEMENTATION
#include "rin/graphics/GraphicsModule.h"

#include "VkBootstrap.h"
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
        
    }

    void GraphicsModule::Shutdown(GRuntime* runtime)
    {
        
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

    Shared<IDeviceImage> GraphicsModule::NewImage(const vk::Extent3D& size, const ImageFormat& format,
        const vk::ImageUsageFlags& usage, bool mips, const std::string& debugName)
    {
        
    }

    Shared<IDeviceBuffer> GraphicsModule::NewBuffer(uint64_t size, const vk::BufferUsageFlags& usage,
        const vk::MemoryPropertyFlags& properties, bool sequentialWrite, bool preferHost, bool mapped,
        const std::string& debugName)
    {
        
    }

    Shared<IDeviceBuffer> GraphicsModule::NewTransferBuffer(uint64_t size, bool sequentialWrite,
        const std::string& debugName)
    {
        return NewBuffer(size,vk::BufferUsageFlagBits::eTransferSrc,vk::MemoryPropertyFlagBits::eHostVisible | vk::MemoryPropertyFlagBits::eHostCoherent,sequentialWrite,true,true,debugName);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewStorageBuffer(uint64_t size, bool sequentialWrite,
        const std::string& debugName)
    {
        return NewBuffer(size,vk::BufferUsageFlagBits::eStorageBuffer | vk::BufferUsageFlagBits::eShaderDeviceAddress,vk::MemoryPropertyFlagBits::eHostVisible,sequentialWrite,false,true,debugName);
    }

    Shared<IDeviceBuffer> GraphicsModule::NewUniformBuffer(uint64_t size, bool sequentialWrite,
        const std::string& debugName)
    {
        return NewBuffer(size,vk::BufferUsageFlagBits::eUniformBuffer,vk::MemoryPropertyFlagBits::eHostVisible,sequentialWrite,false,true,debugName);
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
