#pragma once
#include "GraphImageDescriptor.h"
#include "GraphImagePool.h"
#include "GraphBufferDescriptor.h"
#include "rin/core/memory.h"
#include "rin/rhi/IDeviceBuffer.h"
#include "rin/rhi/IDeviceImage.h"
#include <stack>
namespace rin::rhi
{
    class Frame;
    class Pass;
    class CompiledGraph : public Disposable
    {
        std::stack<Shared<Pass>> _passes{};
        std::unordered_map<uint64_t,GraphImageDescriptor> _imageDescriptors{};
        std::unordered_map<uint64_t,GraphBufferDescriptor> _bufferDescriptors{};
        std::unordered_map<uint64_t,Shared<IDeviceImage>> _images;
        std::unordered_map<uint64_t,Shared<IDeviceBuffer>> _buffers;
        GraphImagePool * _pool{nullptr};
        Frame * _frame;
        Shared<IDeviceBuffer> _buffer;
        uint64_t _bufferOffset{0};
    public:
        CompiledGraph(GraphImagePool * pool,Frame * frame,const std::stack<Shared<Pass>>& passes,const std::unordered_map<uint64_t,GraphImageDescriptor>& imageDescriptors,const std::unordered_map<uint64_t,GraphBufferDescriptor>& bufferDescriptors);
        void Execute();
        Shared<IDeviceImage> GetImage(const uint64_t& id);
        Shared<IDeviceBuffer> GetBuffer(const uint64_t& id);
    protected:
        void OnDispose() override;
    };
}
