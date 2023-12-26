#pragma once
#include "vengine/containers/Array.hpp"
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace rendering {
class PipelineBuilder {

protected:
  Array<vk::PipelineShaderStageCreateInfo> _shaderStages;
  vk::PipelineVertexInputStateCreateInfo _vertexInputInfo;
  vk::PipelineInputAssemblyStateCreateInfo _inputAssembly;
  Array<vk::Viewport> _viewports;
  Array<vk::Rect2D> _scissors;
  vk::PipelineRasterizationStateCreateInfo _rasterizer;
  vk::PipelineColorBlendAttachmentState _colorBlendAttachment;
  vk::PipelineMultisampleStateCreateInfo _multisampling;
  vk::PipelineLayout _layout;
public:
  

  PipelineBuilder& addShaderStage(vk::ShaderModule shader,vk::ShaderStageFlagBits bits = {});

  PipelineBuilder& addVertexShader(vk::ShaderModule shader);

  PipelineBuilder& addFragmentShader(vk::ShaderModule shader);
  
  PipelineBuilder& addViewport(vk::Rect2D rect);

  PipelineBuilder& addScissor(vk::Rect2D scissors);

  PipelineBuilder& vertexInput();

  PipelineBuilder& inputAssembly(vk::PrimitiveTopology topology);

  PipelineBuilder& rasterizer(vk::PolygonMode polygonMode);

  PipelineBuilder& multisampling();

  PipelineBuilder& colorBlendAttachment();

  PipelineBuilder& setLayout(vk::PipelineLayout layout);
  
  vk::Pipeline build(vk::Device device,vk::RenderPass pass);
};
}
}
