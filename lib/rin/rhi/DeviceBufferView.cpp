#include "rin/rhi/DeviceBufferView.h"
namespace rin::rhi
{
    void DeviceBufferView::OnDispose()
    {
        
    }

    DeviceBufferView::DeviceBufferView(const Shared<IDeviceBuffer>& buffer,const uint64_t offset,const uint64_t size)
    {
        _buffer = buffer;
        _offset = offset;
        _size = size;
    }

    uint64_t DeviceBufferView::GetOffset()
    {
        return _buffer->GetOffset() + _offset;
    }

    uint64_t DeviceBufferView::GetSize()
    {
        return _size;
    }

    vk::Buffer DeviceBufferView::GetBuffer()
    {
        return _buffer->GetBuffer();
    }

    uint64_t DeviceBufferView::GetAddress()
    {
        return _buffer->GetAddress() + _offset;
    }

    Shared<IDeviceBuffer> DeviceBufferView::GetView(const uint64_t offset, const uint64_t size)
    {
        return shared<DeviceBufferView>(shared_from_this(), offset, size);
    }

    void DeviceBufferView::Write(void* src, const uint64_t size, const uint64_t offset)
    {
        _buffer->Write(src, size, _buffer->GetOffset() + offset);
    }
}
