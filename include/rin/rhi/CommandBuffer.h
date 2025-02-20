#pragma once
#include <functional>
#include <vulkan/vulkan.hpp>

#include "ImageBarrierOptions.h"
#include "rin/core/math/Vec4.h"

namespace rin::rhi
{
    struct CommandBuffer
    {
        vk::CommandBuffer cmd{};

        operator vk::CommandBuffer() const;
        CommandBuffer& operator=(const vk::CommandBuffer& other);
        CommandBuffer& Apply(const std::function<void(vk::CommandBuffer& cmd)>& operation);
        CommandBuffer& Begin(const vk::CommandBufferUsageFlags& flags = vk::CommandBufferUsageFlagBits::eOneTimeSubmit);
        CommandBuffer& Reset();
        CommandBuffer& End();
        CommandBuffer& DisableMultisampling();
        CommandBuffer& RasterizerDiscard(const bool& enable);
        CommandBuffer& RenderArea(const Vec4<uint32_t>& rect);
        CommandBuffer& PolygonMode(const vk::PolygonMode& mode,float lineWidth = 1.0f);
        CommandBuffer& InputTopology(const vk::PrimitiveTopology& topology);
        CommandBuffer& SetCulling(const vk::CullModeFlags& mode,const vk::FrontFace& frontFace = vk::FrontFace::eClockwise);
        CommandBuffer& DisableCulling();
        CommandBuffer& EnableDepth(const bool& writeEnable,const vk::CompareOp& compareOp);
        CommandBuffer& DisableDepth(const bool& writeEnable = false);
        CommandBuffer& DisableStencil();
        CommandBuffer& EnableBlending(const uint32_t& start,const uint32_t& count,const vk::ColorBlendEquationEXT& equation,const vk::ColorComponentFlags& writeMask);
        CommandBuffer& DisableBlending();
        CommandBuffer& EnableBlendingAdditive(const uint32_t& start,const uint32_t& count);
        CommandBuffer& EnableBlendingAlpha(const uint32_t& start,const uint32_t& count);
        CommandBuffer& PrimitiveRestart(bool enable);
        CommandBuffer& VertexInput(const std::vector<vk::VertexInputBindingDescription2EXT>& bindingDescriptions = {},const std::vector<vk::VertexInputAttributeDescription2EXT>& attributeDescriptions = {});
        CommandBuffer& Draw(const uint32_t& vertexCount, const uint32_t& instanceCount = 1, const uint32_t& firstVertex = 0, const uint32_t& firstInstance = 0);
        CommandBuffer& ImageBarrier(const vk::Image& image,vk::ImageLayout from,
                             vk::ImageLayout to, const ImageBarrierOptions& options = {});
    };
}
