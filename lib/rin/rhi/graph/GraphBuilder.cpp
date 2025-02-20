#include "rin/rhi/graph/GraphBuilder.h"
#include <queue>
#include "rin/rhi/graph/Pass.h"
namespace rin::rhi
{

    GraphBuilder& GraphBuilder::DefineImage(uint64_t& outId,uint32_t width, uint32_t height, ImageFormat format, const vk::ImageLayout& layout)
    {
        outId = _handleGenerator.CreateHandle();
        GraphImageDescriptor descriptor{};
        descriptor.extent = vk::Extent3D{ width, height, 1 };
        descriptor.format = format;
        descriptor.layout = layout;
        descriptor.usage = (format == ImageFormat::Depth || format == ImageFormat::Stencil) ? vk::ImageUsageFlagBits::eDepthStencilAttachment : vk::ImageUsageFlagBits::eStorage | vk::ImageUsageFlagBits::eColorAttachment;
        descriptor.usage |= vk::ImageUsageFlagBits::eTransferDst | vk::ImageUsageFlagBits::eTransferSrc | vk::ImageUsageFlagBits::eSampled;
        
        _imageDescriptors.emplace(outId,descriptor);
        return *this;
    }
    GraphBuilder& GraphBuilder::DefineBuffer(uint64_t& outId,size_t size)
    {
        outId = _handleGenerator.CreateHandle();
        GraphBufferDescriptor descriptor{};
        descriptor.size = size;
        _bufferDescriptors.emplace(outId,descriptor);
        return *this;
    }
    GraphBuilder& GraphBuilder::Read(uint64_t passId, uint64_t id)
    {
        if(!_passesToReads.contains(passId))
        {
            _passesToReads.emplace(passId,std::unordered_set<uint64_t>{});
        }
        if(!_readsToPasses.contains(id))
        {
            _readsToPasses.emplace(id,std::unordered_set<uint64_t>{});
        }
        _passesToReads.at(passId).emplace(id);
        _readsToPasses.at(id).emplace(passId);
        return *this;
    }
    GraphBuilder& GraphBuilder::Write(uint64_t passId, uint64_t id)
    {
        if(!_passesToWrites.contains(passId))
        {
            _passesToWrites.emplace(passId,std::unordered_set<uint64_t>{});
        }
        if(!_writesToPasses.contains(id))
        {
            _writesToPasses.emplace(id,std::unordered_set<uint64_t>{});
        }
        _passesToWrites.at(passId).emplace(id);
        _writesToPasses.at(id).emplace(passId);
        return *this;
    }

    uint64_t GraphBuilder::AddPass(const Shared<Pass>& pass)
    {
        
        auto id = _handleGenerator.CreateHandle();
        pass->_id = id;
        _passes.emplace(id,pass);
        //_nodes.emplace(id,shared<GraphNode>(pass));
        return id;
    }
    Shared<CompiledGraph> GraphBuilder::Compile(Frame* frame, GraphImagePool * pool)
    {

        std::unordered_set<uint64_t> terminalPassIds{};
        for (auto& pass : _passes | std::views::values)
        {
            pass->Configure(*this);
            
            if(pass->IsTerminal())
            {
                terminalPassIds.insert(pass->GetId());
            }
        }

        std::unordered_set<uint64_t> usedResources{};

        std::unordered_set<uint64_t> validPasses{};
        std::stack<Shared<Pass>> passes{};
        
        std::queue<uint64_t> toSearch{};
        
        for (auto &terminalPassId : terminalPassIds)
        {
            toSearch.emplace(terminalPassId);
        }
        
        while(!toSearch.empty())
        {
            auto passId = toSearch.front();
            toSearch.pop();
            if(validPasses.contains(passId)) continue;
            validPasses.emplace(passId);
            passes.emplace(_passes.at(passId));
            auto pass  = _passes.at(passId);
            auto reads = _passesToReads.contains(passId) ? _passesToReads.at(passId) : std::unordered_set<uint64_t>{};
            auto writes = _passesToWrites.contains(passId) ? _passesToWrites.at(passId) : std::unordered_set<uint64_t>{};
            std::unordered_set<uint64_t> dependencies{};
            for (auto &read : reads)
            {
                if(_readsToPasses.contains(read))
                {
                    for (auto &a : _readsToPasses.at(read))
                    {
                        if(!validPasses.contains(a))
                        {
                            usedResources.emplace(read);
                            toSearch.emplace(passId);
                        }
                    }
                }
            }
            for (auto &write : writes)
            {
                usedResources.emplace(write);
            }
        }
        
        std::unordered_map<uint64_t,GraphImageDescriptor> usedImages{};
        std::unordered_map<uint64_t,GraphBufferDescriptor> usedBuffers{};
        for (auto &resource : usedResources)
        {
            if(_imageDescriptors.contains(resource))
            {
                usedImages.emplace(resource, _imageDescriptors.at(resource));
                continue;
            }

            if(_bufferDescriptors.contains(resource))
            {
                usedBuffers.emplace(resource, _bufferDescriptors.at(resource));
            }
        }

        return shared<CompiledGraph>(pool,frame,passes,usedImages,usedBuffers);
    }
}
