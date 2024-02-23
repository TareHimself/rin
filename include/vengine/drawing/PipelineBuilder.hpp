#pragma once
#include "Shader.hpp"
#include "vengine/containers/Array.hpp"
#include <vulkan/vulkan.hpp>

namespace vengine::drawing {
class PipelineBuilder {

protected:
  
  vk::PipelineVertexInputStateCreateInfo _vertexInputInfo{};
  vk::PipelineInputAssemblyStateCreateInfo _inputAssembly{};
  
  vk::PipelineRasterizationStateCreateInfo _rasterizer = vk::PipelineRasterizationStateCreateInfo(
      vk::PipelineRasterizationStateCreateFlags(), vk::False, vk::False, {}, vk::CullModeFlagBits::eBack,
      vk::FrontFace::eClockwise, vk::False, 0.0f, 0.0f, 0.0f, 1.0f);
  
  vk::PipelineColorBlendAttachmentState _colorBlendAttachment{};
  vk::PipelineMultisampleStateCreateInfo _multisampling{};
  vk::PipelineDepthStencilStateCreateInfo _depthStencil{};
  vk::PipelineRenderingCreateInfo _renderInfo{};
  Array<vk::Format> _colorAttachmentFormats;
  Array<Managed<Shader>> _shaders;
  uint32_t _numViewports = 1;
  uint32_t _numScissors = 1;
  
  vk::PipelineLayout _layout;
public:
  

  PipelineBuilder& AddShaderStage(const Managed<Shader> &shader);
  
  PipelineBuilder& SetNumViewports(uint32_t num);

  PipelineBuilder& SetNumScissors(uint32_t num);

  PipelineBuilder& VertexInput(const Array<vk::VertexInputBindingDescription> &
                                   bindings = {},const Array<vk::VertexInputAttributeDescription> &attributes = {});

  PipelineBuilder& SetInputTopology(vk::PrimitiveTopology topology);

  PipelineBuilder& SetPolygonMode(vk::PolygonMode polygonMode);

  PipelineBuilder& SetCullMode(vk::CullModeFlags cullMode, vk::FrontFace frontFace);

  PipelineBuilder& SetMultisamplingModeNone();

  PipelineBuilder& DisableBlending();

  PipelineBuilder& EnableBlendingAdditive();

  PipelineBuilder& EnableBlendingAlphaBlend();

  PipelineBuilder& AddColorAttachment(vk::Format format);

  PipelineBuilder& SetDepthFormat(vk::Format format);

  PipelineBuilder& EnableDepthTest(bool depthWriteEnable,vk::CompareOp op);
  
  PipelineBuilder& DisableDepthTest();

  PipelineBuilder& SetLayout(vk::PipelineLayout layout);
  
  vk::Pipeline Build(vk::Device device);
};
}
