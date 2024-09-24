#include "aerox/graphics/commandBufferUtils.hpp"

#include "aerox/graphics/GraphicsModule.hpp"

void setRenderArea(const vk::CommandBuffer& cmd, const vk::Rect2D& rect)
    {
        vk::Viewport viewport{
            static_cast<float>(rect.offset.x), static_cast<float>(rect.offset.y),
            static_cast<float>(rect.extent.width), static_cast<float>(rect.extent.height),
            0.0f, 1.0f
        };
        cmd.setViewportWithCount(viewport);
        cmd.setScissorWithCount(rect);
    }

    void setRenderExtent(const vk::CommandBuffer& cmd, const vk::Extent2D& extent)
    {
        setRenderArea(cmd, vk::Rect2D{{0, 0}, extent});
    }

    void setPolygonMode(const vk::CommandBuffer& cmd, const vk::PolygonMode& mode, float lineWidth)
    {
        cmd.setPolygonModeEXT(mode,GraphicsModule::dispatchLoader);
        cmd.setLineWidth(lineWidth);
    }

    void disableMultiSampling(const vk::CommandBuffer& cmd)
    {
        cmd.setRasterizationSamplesEXT(vk::SampleCountFlagBits::e1,GraphicsModule::dispatchLoader);
        cmd.setAlphaToCoverageEnableEXT(false,GraphicsModule::dispatchLoader);
        cmd.setAlphaToOneEnableEXT(false,GraphicsModule::dispatchLoader);
        cmd.setSampleMaskEXT(vk::SampleCountFlagBits::e1, 0x1,GraphicsModule::dispatchLoader);
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

    void enableStencilTest(const vk::CommandBuffer& cmd)
    {
        cmd.setStencilTestEnable(true);
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
        cmd.setLogicOpEnableEXT(false,GraphicsModule::dispatchLoader);
        cmd.setLogicOpEXT(vk::LogicOp::eCopy,GraphicsModule::dispatchLoader);
    }

    void disableVertexInput(const vk::CommandBuffer& cmd)
    {
        cmd.setVertexInputEXT(0,nullptr,0,nullptr,GraphicsModule::dispatchLoader);
    }

    void enableBlending(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count,
                        const vk::ColorBlendEquationEXT& equation, vk::ColorComponentFlags writeMask)
    {
        cmd.setLogicOpEnableEXT(false,GraphicsModule::dispatchLoader);
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
        cmd.setColorBlendEnableEXT(start, enables,GraphicsModule::dispatchLoader);
        cmd.setColorBlendEquationEXT(start, equations,GraphicsModule::dispatchLoader);
        cmd.setColorWriteMaskEXT(start, writeMasks,GraphicsModule::dispatchLoader);
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
                        const std::optional<vk::RenderingAttachmentInfo>& depthAttachment,const std::optional<vk::RenderingAttachmentInfo>& stencilAttachment)
    {
        auto renderingInfo = vk::RenderingInfo{{}, renderArea, 1, {}, attachments};
        if (depthAttachment.has_value())
        {
            renderingInfo.setPDepthAttachment(&depthAttachment.value());
        }
        if(stencilAttachment.has_value())
        {
            renderingInfo.setPStencilAttachment(&stencilAttachment.value());
        }
        cmd.beginRendering(renderingInfo);
    }

    void beginRendering(const vk::CommandBuffer& cmd, const vk::Extent2D& renderExtent,
                        const vk::ArrayProxyNoTemporaries<vk::RenderingAttachmentInfo>& attachments,
                        const std::optional<vk::RenderingAttachmentInfo>& depthAttachment,const std::optional<vk::RenderingAttachmentInfo>& stencilAttachment)
    {
        beginRendering(cmd, vk::Rect2D{{0, 0}, renderExtent}, attachments, depthAttachment,stencilAttachment);
    }

    void endRendering(const vk::CommandBuffer& cmd)
    {
        cmd.endRendering();
    }
