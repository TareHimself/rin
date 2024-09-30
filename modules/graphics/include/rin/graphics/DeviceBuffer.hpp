#pragma once

#include <vulkan/vulkan.hpp>

#include "Allocator.hpp"
class DeviceBuffer : public Disposable {
    vk::Buffer _buffer{};
    VmaAllocation _allocation{};
    VmaAllocator _allocator{};
    vk::DeviceSize _size{0};
public:
    DeviceBuffer(VmaAllocator allocator,const vk::Buffer& buffer,VmaAllocation allocation,const vk::DeviceSize& size);

    void OnDispose(bool manual) override;

    vk::Buffer GetBuffer() const;



    void Write(const void * data,const vk::DeviceSize& size,const vk::DeviceSize& offset = 0) const;

    template <typename T>
    void Write(const std::vector<T>& data,const vk::DeviceSize& offset = 0) const;

    template<typename T,typename = std::enable_if_t<!std::is_pointer_v<T>>>
    void Write(T& data,const vk::DeviceSize& offset = 0) const;
    vk::DeviceSize GetSize() const;
};

template <typename T>
void DeviceBuffer::Write(const std::vector<T>& data, const vk::DeviceSize& offset) const
{
    vmaCopyMemoryToAllocation(_allocator,data.data(),_allocation,offset,data.size() * sizeof(T));
}

template <typename T, typename>
void DeviceBuffer::Write(T& data, const vk::DeviceSize& offset) const
{
    vmaCopyMemoryToAllocation(_allocator,&data,_allocation,offset,sizeof(T));
}
