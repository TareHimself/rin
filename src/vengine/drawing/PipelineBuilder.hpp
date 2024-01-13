#pragma once
#include "Shader.hpp"
#include "vengine/containers/Array.hpp"
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace drawing {
class PipelineBuilder {

protected:
  Array<vk::PipelineShaderStageCreateInfo> _shaderStages{};
  vk::PipelineVertexInputStateCreateInfo _vertexInputInfo{};
  vk::PipelineInputAssemblyStateCreateInfo _inputAssembly{};
  
  vk::PipelineRasterizationStateCreateInfo _rasterizer = vk::PipelineRasterizationStateCreateInfo(
      vk::PipelineRasterizationStateCreateFlags(), vk::False, vk::False, {}, vk::CullModeFlagBits::eBack,
      vk::FrontFace::eClockwise, vk::False, 0.0f, 0.0f, 0.0f, 1.0f);
  
  vk::PipelineColorBlendAttachmentState _colorBlendAttachment{};
  vk::PipelineMultisampleStateCreateInfo _multisampling{};
  vk::PipelineDepthStencilStateCreateInfo _depthStencil{};
  vk::PipelineRenderingCreateInfo _renderInfo{};
  vk::Format _colorAttachmentFormat = vk::Format::eUndefined;
  
  uint32_t _numViewports = 1;
  uint32_t _numScissors = 1;
  
  vk::PipelineLayout _layout;
public:
  

  PipelineBuilder& addShaderStage(const Shader * shader,vk::ShaderStageFlagBits bits = {});

  PipelineBuilder& addVertexShader(const Shader * shader);

  PipelineBuilder& addFragmentShader(const Shader * shader);
  
  PipelineBuilder& setNumViewports(uint32_t num);

  PipelineBuilder& setNumScissors(uint32_t num);

  PipelineBuilder& vertexInput(const Array<vk::VertexInputBindingDescription> &
                                   bindings = {},const Array<vk::VertexInputAttributeDescription> &attributes = {});

  PipelineBuilder& setInputTopology(vk::PrimitiveTopology topology);

  PipelineBuilder& setPolygonMode(vk::PolygonMode polygonMode);

  PipelineBuilder& setCullMode(vk::CullModeFlags cullMode, vk::FrontFace frontFace);

  PipelineBuilder& setMultisamplingModeNone();

  PipelineBuilder& disableBlending();

  PipelineBuilder& enableBlendingAdditive();

  PipelineBuilder& enableBlendingAlphaBlend();

  PipelineBuilder& setColorAttachmentFormat(vk::Format format);

  PipelineBuilder& setDepthFormat(vk::Format format);

  PipelineBuilder& enableDepthTest(bool depthWriteEnable,vk::CompareOp op);
  
  PipelineBuilder& disableDepthTest();

  PipelineBuilder& setLayout(vk::PipelineLayout layout);
  
  vk::Pipeline build(vk::Device device);
};
}
}
