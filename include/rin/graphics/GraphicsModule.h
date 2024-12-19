#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "IDeviceBuffer.h"
#include "IDeviceImage.h"
#include "rin/core/Module.h"
#include "rin/io/IoModule.h"
#include "vk_mem_alloc.h"

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

        Shared<IDeviceImage> NewImage(const vk::Extent3D& size,const ImageFormat& format,const vk::ImageUsageFlags& usage, bool mips = false,
        const std::string &debugName = "Image");
        
        Shared<IDeviceBuffer> NewBuffer(uint64_t size,const vk::BufferUsageFlags& usage,const vk::MemoryPropertyFlags& properties,bool sequentialWrite,bool preferHost,bool mapped = false,const std::string &debugName = "Buffer");

        Shared<IDeviceBuffer> NewTransferBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Transfer Buffer");
        Shared<IDeviceBuffer> NewStorageBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Storage Buffer");
        Shared<IDeviceBuffer> NewUniformBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Uniform Buffer");
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


        VmaAllocator _allocator{};
    };
}
