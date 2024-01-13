#include "PipelineBuilder.hpp"

#include "Shader.hpp"
#include "vengine/log.hpp"


namespace vengine {
namespace drawing {
PipelineBuilder &PipelineBuilder::addShaderStage(const Shader *shader,
                                                 vk::ShaderStageFlagBits bits) {
  auto info = vk::PipelineShaderStageCreateInfo(
      vk::PipelineShaderStageCreateFlags(), bits, shader->get(), "main");
  _shaderStages.push(info);
  return *this;
}

PipelineBuilder &PipelineBuilder::addVertexShader(const Shader *shader) {
  return addShaderStage(shader, vk::ShaderStageFlagBits::eVertex);
}

PipelineBuilder &PipelineBuilder::addFragmentShader(const Shader *shader) {
  return addShaderStage(shader, vk::ShaderStageFlagBits::eFragment);
}

PipelineBuilder &PipelineBuilder::setNumViewports(uint32_t num) {

  _numViewports = num;

  return *this;
}

PipelineBuilder &PipelineBuilder::setNumScissors(uint32_t num) {
  _numScissors = num;

  return *this;
}


PipelineBuilder &PipelineBuilder::vertexInput(
    const Array<vk::VertexInputBindingDescription>
    &bindings, const Array<vk::VertexInputAttributeDescription> &attributes) {
  _vertexInputInfo = vk::PipelineVertexInputStateCreateInfo(
      {}, bindings, attributes);
  return *this;
}

PipelineBuilder &
PipelineBuilder::setInputTopology(vk::PrimitiveTopology topology) {
  _inputAssembly.setTopology(topology);
  _inputAssembly.setPrimitiveRestartEnable(false);
  return *this;
}

PipelineBuilder &PipelineBuilder::setPolygonMode(
    vk::PolygonMode polygonMode) {
  _rasterizer.setPolygonMode(polygonMode);
  _rasterizer.setLineWidth(1.0f);
  return *this;
}

PipelineBuilder &PipelineBuilder::setCullMode(vk::CullModeFlags cullMode,
                                              vk::FrontFace frontFace) {
  _rasterizer.setCullMode(cullMode);
  _rasterizer.setFrontFace(frontFace);
  return *this;
}

PipelineBuilder &PipelineBuilder::setMultisamplingModeNone() {
  _multisampling.setSampleShadingEnable(false);
  _multisampling.setRasterizationSamples(vk::SampleCountFlagBits::e1);
  _multisampling.setMinSampleShading(1.0f);
  _multisampling.setPSampleMask(nullptr);
  _multisampling.setAlphaToCoverageEnable(false);
  _multisampling.setAlphaToOneEnable(false);
  return *this;
}

PipelineBuilder &PipelineBuilder::disableBlending() {
  _colorBlendAttachment.setColorWriteMask(
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  _colorBlendAttachment.setBlendEnable(false);

  return *this;
}

PipelineBuilder &PipelineBuilder::enableBlendingAdditive() {
  _colorBlendAttachment.setColorWriteMask(
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  _colorBlendAttachment.setBlendEnable(true);
  _colorBlendAttachment.setSrcColorBlendFactor(vk::BlendFactor::eOne);
  _colorBlendAttachment.setDstColorBlendFactor(
      vk::BlendFactor::eOneMinusDstAlpha);
  _colorBlendAttachment.setColorBlendOp(vk::BlendOp::eAdd);
  _colorBlendAttachment.setSrcAlphaBlendFactor(vk::BlendFactor::eOne);
  _colorBlendAttachment.setDstAlphaBlendFactor(vk::BlendFactor::eZero);
  _colorBlendAttachment.setAlphaBlendOp(vk::BlendOp::eAdd);
  return *this;
}

PipelineBuilder &PipelineBuilder::enableBlendingAlphaBlend() {
  _colorBlendAttachment.setColorWriteMask(
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  _colorBlendAttachment.setBlendEnable(true);
  _colorBlendAttachment.setSrcColorBlendFactor(vk::BlendFactor::eOneMinusDstAlpha);
  _colorBlendAttachment.setDstColorBlendFactor(vk::BlendFactor::eDstAlpha);
  _colorBlendAttachment.setColorBlendOp(vk::BlendOp::eAdd);
  _colorBlendAttachment.setSrcAlphaBlendFactor(vk::BlendFactor::eOne);
  _colorBlendAttachment.setDstAlphaBlendFactor(vk::BlendFactor::eZero);
  _colorBlendAttachment.setAlphaBlendOp(vk::BlendOp::eAdd);
  return *this;
}

PipelineBuilder &PipelineBuilder::setColorAttachmentFormat(vk::Format format) {
  _colorAttachmentFormat = format;
  return *this;
}

PipelineBuilder &PipelineBuilder::setDepthFormat(vk::Format format) {
  _renderInfo.setDepthAttachmentFormat(format);
  _renderInfo.setColorAttachmentFormats(_colorAttachmentFormat);
  return *this;
}

PipelineBuilder &PipelineBuilder::enableDepthTest(bool depthWriteEnable,
                                                  vk::CompareOp op) {
  _depthStencil.setDepthTestEnable(true);
  _depthStencil.setDepthWriteEnable(depthWriteEnable);
  _depthStencil.setDepthCompareOp(op);
  _depthStencil.setDepthBoundsTestEnable(false);
  _depthStencil.setStencilTestEnable(false);
  _depthStencil.setFront({});
  _depthStencil.setBack({});
  _depthStencil.setMinDepthBounds(0.0f);
  _depthStencil.setMaxDepthBounds(1.0f);
  return *this;
}

PipelineBuilder &PipelineBuilder::disableDepthTest() {
  _depthStencil.setDepthTestEnable(false);
  _depthStencil.setDepthWriteEnable(false);
  _depthStencil.setDepthCompareOp(vk::CompareOp::eNever);
  _depthStencil.setDepthBoundsTestEnable(false);
  _depthStencil.setStencilTestEnable(false);
  _depthStencil.setFront({});
  _depthStencil.setBack({});
  _depthStencil.setMinDepthBounds(0.0f);
  _depthStencil.setMaxDepthBounds(1.0f);
  return *this;
}

PipelineBuilder &PipelineBuilder::setLayout(vk::PipelineLayout layout) {
  _layout = layout;

  return *this;
}

vk::Pipeline PipelineBuilder::build(vk::Device device) {

  const auto viewportCreateInfo = vk::PipelineViewportStateCreateInfo(
      vk::PipelineViewportStateCreateFlags(), _numViewports, nullptr,
      _numScissors, nullptr);

  const auto colorBlending = vk::PipelineColorBlendStateCreateInfo(
      vk::PipelineColorBlendStateCreateFlags(), vk::False, vk::LogicOp::eCopy,
      {_colorBlendAttachment});

  vk::DynamicState state[] = {vk::DynamicState::eViewport,
                              vk::DynamicState::eScissor};

  const vk::PipelineDynamicStateCreateInfo dynamicInfo{{}, state};

  // auto format = vk::Format::eR16G16B16A16Sfloat;
  // _renderInfo.setColorAttachmentFormats(format);
  const auto pipelineCreateInfo = vk::GraphicsPipelineCreateInfo()
                                  .setStages(_shaderStages)
                                  .setPVertexInputState(&_vertexInputInfo)
                                  .setPInputAssemblyState(&_inputAssembly)
                                  .setPViewportState(&viewportCreateInfo)
                                  .setPRasterizationState(&_rasterizer)
                                  .setPMultisampleState(&_multisampling)
                                  .setPColorBlendState(&colorBlending)
                                  .setPDepthStencilState(&_depthStencil)

                                  .setPDynamicState(&dynamicInfo)
                                  .setLayout(_layout)
                                  .setPNext(&_renderInfo);

  const auto result = device.
      createGraphicsPipeline(nullptr, pipelineCreateInfo);
  if (result.result != vk::Result::eSuccess) {
    log::drawing->info("Failed to create pipeline");
  }

  return result.value;
}
}
}
