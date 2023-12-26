#include "PipelineBuilder.hpp"

#include "vengine/log.hpp"


namespace vengine {
namespace rendering {
PipelineBuilder &PipelineBuilder::addShaderStage(vk::ShaderModule shader,
                                                 vk::ShaderStageFlagBits bits) {
  auto info = vk::PipelineShaderStageCreateInfo(vk::PipelineShaderStageCreateFlags(), bits, shader, "main");
  _shaderStages.push(info);
  return *this;
}

PipelineBuilder & PipelineBuilder::addVertexShader(vk::ShaderModule shader) {
  return addShaderStage(shader,vk::ShaderStageFlagBits::eVertex);
}

PipelineBuilder & PipelineBuilder::addFragmentShader(vk::ShaderModule shader) {
  return addShaderStage(shader,vk::ShaderStageFlagBits::eFragment);
}

PipelineBuilder &PipelineBuilder::addViewport(vk::Rect2D rect) {
  vk::Viewport viewport;
  viewport.x = rect.offset.x;
  viewport.y = rect.offset.y;
  viewport.width = rect.extent.width;
  viewport.height = rect.extent.height;
  viewport.minDepth = 0.0f;
  viewport.maxDepth = 1.0f;
  _viewports.push(viewport);

  return *this;
}

PipelineBuilder &PipelineBuilder::addScissor(vk::Rect2D scissors) {
  _scissors.push(scissors);

  return *this;
}


PipelineBuilder &PipelineBuilder::vertexInput() {
  _vertexInputInfo = vk::PipelineVertexInputStateCreateInfo({}, 0,{},0,{});
  return *this;
}

PipelineBuilder &
PipelineBuilder::inputAssembly(vk::PrimitiveTopology topology) {
  _inputAssembly = vk::PipelineInputAssemblyStateCreateInfo(
      vk::PipelineInputAssemblyStateCreateFlags(), topology, vk::False);
  return *this;
}

PipelineBuilder &PipelineBuilder::rasterizer(
    vk::PolygonMode polygonMode) {
  _rasterizer = vk::PipelineRasterizationStateCreateInfo(
      vk::PipelineRasterizationStateCreateFlags(), vk::False, vk::False, polygonMode, vk::CullModeFlagBits::eBack,
      vk::FrontFace::eClockwise, vk::False, 0.0f, 0.0f, 0.0f, 1.0f);
  return *this;
}

PipelineBuilder &PipelineBuilder::multisampling() {
  _multisampling = vk::PipelineMultisampleStateCreateInfo(
      vk::PipelineMultisampleStateCreateFlags(), vk::SampleCountFlagBits::e1, vk::False, 1.0f, nullptr, vk::False,
      vk::False);
  return *this;
}

PipelineBuilder &PipelineBuilder::colorBlendAttachment() {
  _colorBlendAttachment = vk::PipelineColorBlendAttachmentState(
      vk::False, vk::BlendFactor::eOne, vk::BlendFactor::eZero, vk::BlendOp::eAdd, vk::BlendFactor::eOne, vk::BlendFactor::eZero, vk::BlendOp::eAdd,
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  return *this;
}

PipelineBuilder & PipelineBuilder::setLayout(vk::PipelineLayout layout) {
  _layout = layout;

  return *this;
}

vk::Pipeline PipelineBuilder::build(vk::Device device, vk::RenderPass pass) {

  const auto viewportCreateInfo = vk::PipelineViewportStateCreateInfo(
      vk::PipelineViewportStateCreateFlags(), _viewports, _scissors);

  const auto colorBlending = vk::PipelineColorBlendStateCreateInfo(
      vk::PipelineColorBlendStateCreateFlags(), vk::False, vk::LogicOp::eCopy, {_colorBlendAttachment});

  const auto pipelineCreateInfo = vk::GraphicsPipelineCreateInfo(
      {}, _shaderStages, &_vertexInputInfo, &_inputAssembly, {},
      &viewportCreateInfo, &_rasterizer, &_multisampling, {}, &colorBlending,
      {},_layout,pass,0);

  const auto result = device.createGraphicsPipeline(nullptr,pipelineCreateInfo);
  if(result.result != vk::Result::eSuccess) {
    log::rendering->info("Failed to create pipeline");
  }
  return result.value;
}
}
}
