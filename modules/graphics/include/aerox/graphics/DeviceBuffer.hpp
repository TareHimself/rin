#pragma once

#include <vulkan/vulkan.hpp>

#include "Allocator.hpp"
namespace aerox::graphics
{
    class DeviceBuffer {
        vk::Buffer _buffer{};
        VmaAllocation _allocation{};
        VmaAllocator _allocator{};
        vk::DeviceSize _size{0};
    public:
        DeviceBuffer(VmaAllocator allocator,const vk::Buffer& buffer,VmaAllocation allocation,const vk::DeviceSize& size);

        ~DeviceBuffer();

        vk::Buffer GetBuffer() const;

        void Write(const void * data,const vk::DeviceSize& size,const vk::DeviceSize& offset = 0) const;

        void Write(const std::vector<std::byte>& data,const vk::DeviceSize& offset = 0) const;
    };
}
