#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "rin/core/Module.h"
#include "rin/io/IoModule.h"


namespace rin::graphics
{
    class GraphicsModule : public Module
    {
        io::IoModule * _ioModule{};
        bool IsDependentOn(Module* module) override;


        
    protected:
        void OnDispose() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;

    public:
        std::string GetName() override;

    protected:
        void RegisterRequiredModules(GRuntime* runtime) override;

    private:

        vk::Instance _instance{};
        vk::Device _device{};
        vk::PhysicalDevice _physicalDevice{};
        vk::Queue _graphicsQueue{};
        uint32_t _graphicsQueueIndex{0};
        vk::Queue _transferQueue{};
        uint32_t _transferQueueIndex{0};

        vk::CommandPool _immediateCommandPool{};
        vk::CommandBuffer _immediateCommandBuffer{};
        vk::Fence _immediateFence{};
        
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        vk::DebugUtilsMessengerEXT _debugUtilsMessengerExt{};
#endif
        
    };
}
