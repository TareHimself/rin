#include "aerox/graphics/DeviceBuffer.hpp"
namespace aerox::graphics
{
    DeviceBuffer::DeviceBuffer(VmaAllocator allocator,const vk::Buffer& buffer, VmaAllocation allocation, const vk::DeviceSize& size)
    {
        _buffer = buffer;
        _allocation = allocation;
        _allocator = allocator;
        _size = size;
    }

    DeviceBuffer::~DeviceBuffer()
    {
        vmaDestroyBuffer(_allocator,_buffer,_allocation);
    }

    vk::Buffer DeviceBuffer::GetBuffer() const
    {
        return _buffer;
    }

    void DeviceBuffer::Write(const void* data, const vk::DeviceSize& size, const vk::DeviceSize& offset) const
    {
        vmaCopyMemoryToAllocation(_allocator,data,_allocation,offset,size);
    }

    void DeviceBuffer::Write(const std::vector<std::byte>& data, const vk::DeviceSize& offset) const
    {
        vmaCopyMemoryToAllocation(_allocator,data.data(),_allocation,offset,data.size());
    }
}