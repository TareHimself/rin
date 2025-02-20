#pragma once
#include <cstdint>
#include <vulkan/vulkan.hpp>
#include <map>
#include <unordered_set>
#include <unordered_map>

#include "CompiledGraph.h"
#include "GraphImageDescriptor.h"
#include "GraphBufferDescriptor.h"
#include "GraphImagePool.h"
#include "rin/core/HandleGenerator.h"
#include "rin/core/memory.h"
#include "rin/rhi/Frame.h"
#include "rin/rhi/ImageFormat.h"

namespace rin::rhi
{
    class Pass;
    class GraphBuilder
    {
        struct GraphNode
        {
            Shared<Pass> pass;
            std::unordered_set<uint64_t> writes;
            std::unordered_set<uint64_t> reads;
            bool active{};
        };
        HandleGenerator _handleGenerator{};
        std::unordered_map<uint64_t,GraphImageDescriptor> _imageDescriptors{};
        std::unordered_map<uint64_t,GraphBufferDescriptor> _bufferDescriptors{};
        std::unordered_map<uint64_t,Shared<Pass>> _passes{};
        std::unordered_map<uint64_t,std::unordered_set<uint64_t>> _passesToReads{};
        std::unordered_map<uint64_t,std::unordered_set<uint64_t>> _passesToWrites{};
        std::unordered_map<uint64_t,std::unordered_set<uint64_t>> _readsToPasses{};
        std::unordered_map<uint64_t,std::unordered_set<uint64_t>> _writesToPasses{};
        //std::unordered_map<uint64_t,Shared<GraphNode>> _nodes{};
    public:
        GraphBuilder& DefineImage(uint64_t& outId,uint32_t width,uint32_t height,ImageFormat format,const vk::ImageLayout& layout);
        GraphBuilder& DefineBuffer(uint64_t& outId,size_t size);

        GraphBuilder& Read(uint64_t passId,uint64_t id);
        GraphBuilder& Write(uint64_t passId,uint64_t id);

        uint64_t AddPass(const Shared<Pass>& pass);

        template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_base_of_v<Pass,T> && std::is_constructible_v<T,TArgs...>,uint64_t> AddPass(TArgs&&... args);

        Shared<CompiledGraph> Compile(Frame * frame,GraphImagePool * pool);
    };
    template <typename T, typename ... TArgs>
    std::enable_if_t<std::is_base_of_v<Pass, T> && std::is_constructible_v<T, TArgs...>, uint64_t> GraphBuilder::AddPass(TArgs&&... args)
    {
        return AddPass(shared<T>(std::forward<TArgs>(args)...));
    }

}
