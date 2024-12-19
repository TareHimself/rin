#pragma once
#include "IDeviceBuffer.h"

namespace rin::graphics
{
    class DeviceBufferView : public IDeviceBuffer, public std::enable_shared_from_this<DeviceBufferView>
    {
        Shared<IDeviceBuffer> _buffer{};
        uint64_t _offset = 0;
        uint64_t _size = 0;
    protected:
        void OnDispose() override;

    public:
        DeviceBufferView(const Shared<IDeviceBuffer>& buffer,const uint64_t offset,const uint64_t size);
        uint64_t GetOffset() override;
        uint64_t GetSize() override;
        vk::Buffer GetBuffer() override;
        uint64_t GetAddress() override;
        Shared<IDeviceBuffer> GetView(const uint64_t offset, const uint64_t size) override;
        void Write(void* src, const uint64_t offset, const uint64_t size) override;
    };
}
