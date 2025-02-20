#include "rin/rhi/DeviceBuffer.h"
#include "vk_mem_alloc.h"
#include "rin/core/macros.h"
#include "rin/rhi/DeviceBufferView.h"
#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{
    void DeviceBuffer::OnDispose()
    {
        vmaDestroyBuffer(_allocator,_buffer,_allocation);
    }

    DeviceBuffer::DeviceBuffer(const VmaAllocator& allocator, const VmaAllocation& allocation, const vk::Buffer& buffer,
        const uint64_t& size)
    {
        _allocator = allocator;
        _allocation = allocation;
        _buffer = buffer;
        _size = size;
    }

    uint64_t DeviceBuffer::GetOffset()
    {
        return 0;
    }

    uint64_t DeviceBuffer::GetSize()
    {
        return _size;
    }

    vk::Buffer DeviceBuffer::GetBuffer()
    {
        return _buffer;
    }

    uint64_t DeviceBuffer::GetAddress()
    {
        NOT_IMPLEMENTED
    }

    Shared<IDeviceBuffer> DeviceBuffer::GetView(uint64_t offset, uint64_t size)
    {
        return shared<DeviceBufferView>(shared_from_this(), offset, size);
    }

    void DeviceBuffer::Write(void* src, uint64_t size, uint64_t offset)
    {
        vmaCopyMemoryToAllocation(_allocator,src,_allocation,offset,size);
    }
}
