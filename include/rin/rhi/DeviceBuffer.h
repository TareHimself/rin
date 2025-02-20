#pragma once
#include "IDeviceBuffer.h"
#include "vk_mem_alloc.h"
namespace rin::rhi
{
    
    class DeviceBuffer : public IDeviceBuffer, std::enable_shared_from_this<DeviceBuffer>
    {
        vk::Buffer _buffer{};
        uint64_t _size = 0;
        VmaAllocation _allocation{};
        VmaAllocator _allocator{};
    protected:
        void OnDispose() override;

    public:
        DeviceBuffer(const VmaAllocator& allocator,const VmaAllocation& allocation,const vk::Buffer& buffer,const uint64_t& size);
        uint64_t GetOffset() override;
        uint64_t GetSize() override;
        vk::Buffer GetBuffer() override;
        uint64_t GetAddress() override;
        Shared<IDeviceBuffer> GetView(uint64_t offset, uint64_t size) override;
        void Write(void* src, uint64_t size, uint64_t offset) override;
    };
}
