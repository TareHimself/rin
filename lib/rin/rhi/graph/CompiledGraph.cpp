#include "rin/rhi/graph/CompiledGraph.h"

#include "rin/rhi/graph/Pass.h"
namespace rin::rhi
{
  
    CompiledGraph::CompiledGraph(GraphImagePool* pool,Frame * frame, const std::stack<Shared<Pass>>& passes, const std::unordered_map<uint64_t, GraphImageDescriptor>& imageDescriptors,
        const std::unordered_map<uint64_t, GraphBufferDescriptor>& bufferDescriptors)
    {
        
        _pool = pool;
        _frame = frame;
        _passes = passes;
        _imageDescriptors = imageDescriptors;
        _bufferDescriptors = bufferDescriptors;
        uint64_t totalMemory{0};

        for (const auto & [fst, snd] : _bufferDescriptors)
        {
            totalMemory += snd.size;
        }

        if(totalMemory > 0)
        {
            _buffer = GraphicsModule::Get()->NewStorageBuffer(totalMemory,false,"Graph Buffer");
        }
    }
    void CompiledGraph::Execute()
    {
        std::stack<Shared<Pass>> passes{_passes};
        while(!_passes.empty())
        {
            auto pass = _passes.top();
            _passes.pop();
            pass->Execute(_frame,this);
        }
    }
    Shared<IDeviceImage> CompiledGraph::GetImage(const uint64_t& id)
    {
        if(const auto iter = _imageDescriptors.find(id); iter != _imageDescriptors.end())
        {
            if(_images.contains(id))
            {
                return _images.at(id);
            }
            auto &descriptor = iter->second;
            return _images.emplace(descriptor.ComputeHashCode(),_pool->AllocateImage(descriptor,_frame)).first->second;
        }
        throw std::runtime_error("Unknown Image Id");
    }
    Shared<IDeviceBuffer> CompiledGraph::GetBuffer(const uint64_t& id)
    {
        if(const auto iter = _bufferDescriptors.find(id); iter != _bufferDescriptors.end())
        {
            if(_buffer)
            {
                if(_buffers.contains(id))
                {
                    return _buffers.at(id);
                }
                
                auto data = _buffer->GetView(_bufferOffset,iter->second.size);
                _bufferOffset += data->GetSize();
                return _buffers.emplace(id,data).first->second;
            }
            throw std::runtime_error("Failed to allocate buffer");
        }
        throw std::runtime_error("Unknown Image Id");
    }
    void CompiledGraph::OnDispose()
    {
        _images.clear();
        _buffers.clear();
    }
}
