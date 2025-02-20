#include "rin/rhi/CommandBuffer.h"

#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{
    CommandBuffer::operator vk::CommandBuffer() const
    {
        return cmd;
    }
    CommandBuffer& CommandBuffer::operator=(const vk::CommandBuffer& other)
    {
        cmd = other;
        return *this;
    }
    
    CommandBuffer& CommandBuffer::Apply(const std::function<void(vk::CommandBuffer& cmd)>& operation)
    {
        operation(cmd);
        return *this;
    }
    CommandBuffer& CommandBuffer::Begin(const vk::CommandBufferUsageFlags& flags)
    {
        cmd.begin(flags);
        return *this;
    }
    CommandBuffer& CommandBuffer::Reset()
    {
        cmd.reset();
        return *this;
    }
    CommandBuffer& CommandBuffer::End()
    {
        cmd.end();
        return *this;
    }
    CommandBuffer& CommandBuffer::DisableMultisampling()
    {
        cmd.setRasterizationSamplesEXT(vk::SampleCountFlagBits::e1,GraphicsModule::GetDispatchLoader());
        cmd.setAlphaToCoverageEnableEXT(false,GraphicsModule::GetDispatchLoader());
        cmd.setAlphaToOneEnableEXT(false,GraphicsModule::GetDispatchLoader());
        cmd.setSampleMaskEXT(vk::SampleCountFlagBits::e1,0x1,GraphicsModule::GetDispatchLoader());
        return *this;
    }
    CommandBuffer& CommandBuffer::RasterizerDiscard(const bool& enable)
    {
        cmd.setRasterizerDiscardEnable(enable);
        return *this;
    }
    CommandBuffer& CommandBuffer::RenderArea(const Vec4<uint32_t>& rect)
    {

        return *this;
    }
    CommandBuffer& CommandBuffer::PolygonMode(const vk::PolygonMode& mode, float lineWidth)
    {
        cmd.setPolygonModeEXT(mode,GraphicsModule::GetDispatchLoader());
        cmd.setLineWidth(lineWidth);
        return *this;
    }
    CommandBuffer& CommandBuffer::InputTopology(const vk::PrimitiveTopology& topology)
    {
        cmd.setPrimitiveTopology(topology);
        PrimitiveRestart(false);
        return *this;
    }
    CommandBuffer& CommandBuffer::SetCulling(const vk::CullModeFlags& mode, const vk::FrontFace& frontFace)
    {
        cmd.setCullMode(mode);
        cmd.setFrontFace(frontFace);
        return *this;
    }
    CommandBuffer& CommandBuffer::DisableCulling()
    {
        SetCulling(vk::CullModeFlagBits::eNone);
        return *this;
    }
    CommandBuffer& CommandBuffer::EnableDepth(const bool& writeEnable, const vk::CompareOp& compareOp)
    {
        cmd.setDepthTestEnable(true);
        cmd.setDepthWriteEnable(writeEnable);
        cmd.setDepthCompareOp(compareOp);
        cmd.setDepthBiasEnable(false);
        cmd.setDepthBoundsTestEnable(false);
        return *this;
    }
    CommandBuffer& CommandBuffer::DisableDepth(const bool& writeEnable)
    {
        cmd.setDepthTestEnable(false);
        cmd.setDepthWriteEnable(writeEnable);
        cmd.setDepthBiasEnable(false);
        cmd.setDepthBoundsTestEnable(false);
        return *this;
    }
    CommandBuffer& CommandBuffer::DisableStencil()
    {
        cmd.setStencilTestEnable(false);
        return *this;
    }
    CommandBuffer& CommandBuffer::EnableBlending(const uint32_t& start, const uint32_t& count, const vk::ColorBlendEquationEXT& equation, const vk::ColorComponentFlags& writeMask)
    {
        std::vector<vk::Bool32> enables{};
        std::vector<vk::ColorBlendEquationEXT> equations{};
        std::vector<vk::ColorComponentFlags> writeMasks{};
        enables.reserve(count);
        equations.reserve(count);
        writeMasks.reserve(count);
        for(uint32_t i = 0; i < count; ++i)
        {
            enables.emplace_back(true);
            equations.emplace_back(equation);
            writeMasks.emplace_back(writeMask);
        }
        cmd.setColorBlendEnableEXT(start,enables,GraphicsModule::GetDispatchLoader());
        cmd.setColorBlendEquationEXT(start,equations,GraphicsModule::GetDispatchLoader());
        cmd.setColorWriteMaskEXT(start,writeMask,GraphicsModule::GetDispatchLoader());
        return *this;
    }
    CommandBuffer& CommandBuffer::DisableBlending()
    {
        cmd.setLogicOpEnableEXT(false,GraphicsModule::GetDispatchLoader());
        cmd.setLogicOpEXT(vk::LogicOp::eCopy,GraphicsModule::GetDispatchLoader());
        return *this;
    }
    CommandBuffer& CommandBuffer::EnableBlendingAdditive(const uint32_t& start, const uint32_t& count)
    {
        return EnableBlending(
            start,
            count,
            vk::ColorBlendEquationEXT{
                vk::BlendFactor::eOne,
                vk::BlendFactor::eOneMinusDstAlpha,
                vk::BlendOp::eAdd,
                vk::BlendFactor::eOne,
                vk::BlendFactor::eZero,
                vk::BlendOp::eAdd
            },
            vk::ColorComponentFlagBits::eR |
            vk::ColorComponentFlagBits::eG |
            vk::ColorComponentFlagBits::eB |
            vk::ColorComponentFlagBits::eA
        );
    }
    CommandBuffer& CommandBuffer::EnableBlendingAlpha(const uint32_t& start, const uint32_t& count)
    {
        return EnableBlending(
                   start,
                   count,
                   vk::ColorBlendEquationEXT{
                       vk::BlendFactor::eOne,
                       vk::BlendFactor::eOneMinusDstAlpha,
                       vk::BlendOp::eAdd,
                       vk::BlendFactor::eOne,
                       vk::BlendFactor::eOneMinusSrcAlpha,
                       vk::BlendOp::eSubtract
                   },
                   vk::ColorComponentFlagBits::eR |
                   vk::ColorComponentFlagBits::eG |
                   vk::ColorComponentFlagBits::eB |
                   vk::ColorComponentFlagBits::eA
               );
    }
    CommandBuffer& CommandBuffer::PrimitiveRestart(const bool enable)
    {
        cmd.setPrimitiveRestartEnable(enable);
        return *this;
    }
    CommandBuffer& CommandBuffer::VertexInput(const std::vector<vk::VertexInputBindingDescription2EXT>& bindingDescriptions,
        const std::vector<vk::VertexInputAttributeDescription2EXT>& attributeDescriptions)
    {
        cmd.setVertexInputEXT(bindingDescriptions,attributeDescriptions,GraphicsModule::GetDispatchLoader());
        return *this;
    }
    CommandBuffer& CommandBuffer::Draw(const uint32_t& vertexCount, const uint32_t& instanceCount, const uint32_t& firstVertex, const uint32_t& firstInstance)
    {
        cmd.draw(vertexCount,instanceCount,firstVertex,firstInstance);
        return *this;
    }
    CommandBuffer& CommandBuffer::ImageBarrier(const vk::Image& image, const vk::ImageLayout from, const vk::ImageLayout to, const ImageBarrierOptions& options)
    {
        vk::ImageMemoryBarrier2 data[] = {
            vk::ImageMemoryBarrier2{
                options.waitForStages,
                options.srcAccessFlags,
                options.nextStages,
                options.dstAccessFlags,
                from,
                to,
                {},
                {},
                image,
                options.subresourceRange
            }
        };
        cmd.pipelineBarrier2({
            {}, {}, {}, data
        });
        return *this;
    }
}
