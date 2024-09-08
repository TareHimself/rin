#include "aerox/graphics/commandBufferUtils.hpp"

namespace aerox::graphics
{
    void setRenderArea(const vk::CommandBuffer& cmd, const vk::Rect2D& rect)
    {
        cmd.setViewport(0, vk::Viewport{
                            static_cast<float>(rect.offset.x), static_cast<float>(rect.offset.y),
                            static_cast<float>(rect.extent.width), static_cast<float>(rect.extent.height),
                            0.0f, 1.0f
                        });
        cmd.setScissor(0, rect);
    }

    void setRenderExtent(const vk::CommandBuffer& cmd, const vk::Extent2D& extent)
    {
        setRenderArea(cmd, vk::Rect2D{{0, 0}, extent});
    }

    void setPolygonMode(const vk::CommandBuffer& cmd, const vk::PolygonMode& mode, float lineWidth)
    {
        cmd.setPolygonModeEXT(mode);
        cmd.setLineWidth(lineWidth);
    }

    void disableMultiSampling(const vk::CommandBuffer& cmd)
    {
        cmd.setRasterizationSamplesEXT(vk::SampleCountFlagBits::e1);
        cmd.setAlphaToCoverageEnableEXT(false);
        cmd.setAlphaToOneEnableEXT(false);
        cmd.setSampleMaskEXT(vk::SampleCountFlagBits::e1, 0x1);
    }

    void enableRasterizerDiscard(const vk::CommandBuffer& cmd)
    {
        cmd.setRasterizerDiscardEnable(true);
    }

    void disableRasterizerDiscard(const vk::CommandBuffer& cmd)
    {
        cmd.setRasterizerDiscardEnable(false);
    }

    void setInputTopology(const vk::CommandBuffer& cmd, vk::PrimitiveTopology topology)
    {
        cmd.setPrimitiveTopology(topology);
        cmd.setPrimitiveRestartEnable(false);
    }

    void setCullMode(const vk::CommandBuffer& cmd, vk::CullModeFlagBits cullMode, vk::FrontFace frontFace)
    {
        cmd.setCullMode(cullMode);
        cmd.setFrontFace(frontFace);
    }

    void enableDepthTest(const vk::CommandBuffer& cmd, bool depthWriteEnable, vk::CompareOp compareOp)
    {
        cmd.setDepthTestEnable(true);
        cmd.setDepthWriteEnable(depthWriteEnable);
        cmd.setDepthCompareOp(compareOp);
        cmd.setDepthBiasEnable(false);
        cmd.setDepthBoundsTestEnable(false);
    }

    void disableDepthTest(const vk::CommandBuffer& cmd, bool depthWriteEnable)
    {
        cmd.setDepthTestEnable(false);
        cmd.setDepthWriteEnable(depthWriteEnable);
        cmd.setDepthBiasEnable(false);
        cmd.setDepthBoundsTestEnable(false);
    }

    void disableStencilTest(const vk::CommandBuffer& cmd)
    {
        cmd.setStencilTestEnable(false);
    }

    void disableCulling(const vk::CommandBuffer& cmd)
    {
        setCullMode(cmd, vk::CullModeFlagBits::eNone, vk::FrontFace::eClockwise);
    }

    void disableBlending(const vk::CommandBuffer& cmd)
    {
        cmd.setLogicOpEnableEXT(false);
        cmd.setLogicOpEXT(vk::LogicOp::eCopy);
    }

    void enableBlending(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count,
                        const vk::ColorBlendEquationEXT& equation, vk::ColorComponentFlags writeMask)
    {
        cmd.setLogicOpEnableEXT(false);
        std::vector<vk::Bool32> enables{};
        std::vector<vk::ColorBlendEquationEXT> equations{};
        std::vector<vk::ColorComponentFlags> writeMasks{};
        enables.reserve(count);
        equations.reserve(count);
        writeMasks.reserve(count);
        for (auto i = 0; i < count; i++)
        {
            enables.push_back(true);
            equations.push_back(equation);
            writeMasks.push_back(writeMask);
        }
        cmd.setColorBlendEnableEXT(start, enables);
        cmd.setColorBlendEquationEXT(start, equations);
        cmd.setColorWriteMaskEXT(start, writeMasks);
    }

    void enableBlendingAdditive(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count)
    {
        enableBlending(cmd, start, count, vk::ColorBlendEquationEXT{vk::BlendFactor::eOne,vk::BlendFactor::eOneMinusDstAlpha,vk::BlendOp::eAdd,
                           vk::BlendFactor::eOne,
                           vk::BlendFactor::eZero,
                           vk::BlendOp::eAdd
                       },
                       vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG | vk::ColorComponentFlagBits::eB
                       | vk::ColorComponentFlagBits::eA);
    }

    void enableBlendingAlphaBlend(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count)
    {
        enableBlending(cmd, start, count, vk::ColorBlendEquationEXT{
                           vk::BlendFactor::eSrcAlpha,
                           vk::BlendFactor::eOneMinusDstAlpha,
                           vk::BlendOp::eAdd,
                           vk::BlendFactor::eSrcAlpha,
                           vk::BlendFactor::eOneMinusSrcAlpha,
                           vk::BlendOp::eSubtract
                       },
                       vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG | vk::ColorComponentFlagBits::eB
                       | vk::ColorComponentFlagBits::eA);
    }

    void beginRendering(const vk::CommandBuffer& cmd, const vk::Rect2D& renderArea,
                        const vk::ArrayProxyNoTemporaries<vk::RenderingAttachmentInfo>& attachments,
                        const std::optional<vk::RenderingAttachmentInfo>& depthAttachment)
    {
        auto renderingInfo = vk::RenderingInfo{{}, renderArea, {}, {}, attachments};
        if (depthAttachment.has_value())
        {
            renderingInfo.setPDepthAttachment(&depthAttachment.value());
        }
        cmd.beginRendering(renderingInfo);
    }

    void beginRendering(const vk::CommandBuffer& cmd, const vk::Extent2D& renderExtent,
                        const vk::ArrayProxyNoTemporaries<vk::RenderingAttachmentInfo>& attachments,
                        const std::optional<vk::RenderingAttachmentInfo>& depthAttachment)
    {
        beginRendering(cmd, vk::Rect2D{{0, 0}, renderExtent}, attachments, depthAttachment);
    }

    void endRendering(const vk::CommandBuffer& cmd)
    {
        cmd.endRendering();
    }
}
