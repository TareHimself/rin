#pragma once
#include <vulkan/vulkan.hpp>
#include <optional>
void setRenderArea(const vk::CommandBuffer& cmd, const vk::Rect2D& rect);
void setRenderExtent(const vk::CommandBuffer& cmd, const vk::Extent2D& extent);
void setPolygonMode(const vk::CommandBuffer& cmd, const vk::PolygonMode& mode, float lineWidth = 1.0f);
void disableMultiSampling(const vk::CommandBuffer& cmd);
void enableRasterizerDiscard(const vk::CommandBuffer& cmd);
void disableRasterizerDiscard(const vk::CommandBuffer& cmd);
void setInputTopology(const vk::CommandBuffer& cmd, vk::PrimitiveTopology topology);
void setCullMode(const vk::CommandBuffer& cmd, vk::CullModeFlagBits cullMode, vk::FrontFace frontFace);
void enableDepthTest(const vk::CommandBuffer& cmd, bool depthWriteEnable, vk::CompareOp compareOp);
void disableDepthTest(const vk::CommandBuffer& cmd, bool depthWriteEnable = false);
void enableStencilTest(const vk::CommandBuffer& cmd);
void disableStencilTest(const vk::CommandBuffer& cmd);
void disableCulling(const vk::CommandBuffer& cmd);
void disableBlending(const vk::CommandBuffer& cmd);
void disableVertexInput(const vk::CommandBuffer& cmd);
void enableBlending(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count,
                    const vk::ColorBlendEquationEXT& equation, vk::ColorComponentFlags writeMask);
void enableBlendingAdditive(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count);
void enableBlendingAlphaBlend(const vk::CommandBuffer& cmd, uint32_t start, uint32_t count);
void beginRendering(const vk::CommandBuffer& cmd, const vk::Rect2D& renderArea,
                    const vk::ArrayProxyNoTemporaries<vk::RenderingAttachmentInfo>& attachments,
                    const std::optional<vk::RenderingAttachmentInfo>& depthAttachment = {},
                    const std::optional<vk::RenderingAttachmentInfo>& stencilAttachment = {});
void beginRendering(const vk::CommandBuffer& cmd, const vk::Extent2D& renderExtent,
                    const vk::ArrayProxyNoTemporaries<vk::RenderingAttachmentInfo>& attachments,
                    const std::optional<vk::RenderingAttachmentInfo>& depthAttachment = {},
                    const std::optional<vk::RenderingAttachmentInfo>& stencilAttachment = {});
void endRendering(const vk::CommandBuffer& cmd);
