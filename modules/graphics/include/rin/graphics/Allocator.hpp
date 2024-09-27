#pragma once
#include <vk_mem_alloc.h>
#include <vulkan/vulkan.hpp>
#include "rin/core/memory.hpp"
#include "shaders/ShaderResource.hpp"

 class Shader;
    class DeviceImage;
    class DeviceBuffer;
    class GraphicsModule;
    class Shader;

    class Allocator {
        VmaAllocator _allocator{};
    public:
        explicit Allocator(const GraphicsModule  * module);
        ~Allocator();

        Shared<DeviceBuffer> NewBuffer(vk::DeviceSize size, vk::BufferUsageFlags usageFlags, vk::MemoryPropertyFlags propertyFlags,
            bool sequentialWrite = true, bool preferHost = false, bool mapped = false, const std::string& debugName = "Buffer");

        template<typename T>
        Shared<DeviceBuffer> NewBuffer(vk::BufferUsageFlags usageFlags, vk::MemoryPropertyFlags propertyFlags,
            bool sequentialWrite = true, bool preferHost = false, bool mapped = false, const std::string& debugName = "Buffer");

        Shared<DeviceBuffer> NewTransferBuffer(vk::DeviceSize size,bool sequentialWrite = true,const std::string& debugName = "Transfer Buffer");

        Shared<DeviceBuffer> NewUniformBuffer(vk::DeviceSize size,bool sequentialWrite = true,const std::string& debugName = "Uniform Buffer");

        Shared<DeviceBuffer> NewStorageBuffer(vk::DeviceSize size,bool sequentialWrite = true,const std::string& debugName = "Storage Buffer");

        Shared<DeviceBuffer> NewResourceBuffer(const ShaderResource& resource,bool sequentialWrite = true,const std::string& debugName = {});

        template<typename T>
        Shared<DeviceBuffer> NewTransferBuffer(bool sequentialWrite = true,const std::string& debugName = "Transfer Buffer");

        template<typename T>
        Shared<DeviceBuffer> NewUniformBuffer(bool sequentialWrite = true,const std::string& debugName = "Uniform Buffer");

        template<typename T>
        Shared<DeviceBuffer> NewStorageBuffer(bool sequentialWrite = true,const std::string& debugName = "Storage Buffer");

        Shared<DeviceImage> NewImage(const vk::ImageCreateInfo& createInfo, const std::string& debugName = "Buffer");
    };

    template <typename T>
    Shared<DeviceBuffer> Allocator::NewBuffer(const vk::BufferUsageFlags usageFlags, const vk::MemoryPropertyFlags propertyFlags,
                                              const bool sequentialWrite, const bool preferHost, const bool mapped, const std::string& debugName)
    {
        return NewBuffer(sizeof(T),usageFlags,propertyFlags,sequentialWrite,preferHost,mapped,debugName);
    }

    template <typename T>
    Shared<DeviceBuffer> Allocator::NewTransferBuffer(const bool sequentialWrite, const std::string& debugName)
    {
        return NewTransferBuffer(sizeof(T),sequentialWrite,debugName);
    }

    template <typename T>
    Shared<DeviceBuffer> Allocator::NewUniformBuffer(const bool sequentialWrite, const std::string& debugName)
    {
        return NewUniformBuffer(sizeof(T),sequentialWrite,debugName);
    }

    template <typename T>
    Shared<DeviceBuffer> Allocator::NewStorageBuffer(bool sequentialWrite, const std::string& debugName)
    {
        return NewStorageBuffer(sizeof(T),sequentialWrite,debugName);
    }