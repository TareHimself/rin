#pragma once
#include "vulkan/vulkan.hpp"
#include "graph/IGraphResource.h"
#include "rin/core/Disposable.h"
#include "rin/core/memory.h"

namespace rin::rhi
{
    class IDeviceBuffer : public Disposable, public IGraphResource
    {
    public:
        virtual uint64_t GetOffset() = 0;
        virtual uint64_t GetSize() = 0;
        virtual vk::Buffer GetBuffer() = 0;
        virtual uint64_t GetAddress() = 0;
        virtual Shared<IDeviceBuffer> GetView(uint64_t offset, uint64_t size) = 0;
        virtual void Write(void * src,uint64_t size,uint64_t offset = 0) = 0;
        
        template<typename T>
        void Write(const std::vector<T>& data,uint64_t offset = 0);

        template<typename T>
        void Write(T& data,uint64_t offset = 0);
    };

    template <typename T>
    void IDeviceBuffer::Write(const std::vector<T>& data,const uint64_t offset)
    {
        Write(data.data(),data.size() * sizeof(T),offset);
    }

    template <typename T>
    void IDeviceBuffer::Write( T& data,const uint64_t offset)
    {
        Write(&data,sizeof(T),offset);
    }
}
