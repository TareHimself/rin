#include <vengine/drawing/PipelineBuilder.hpp>
#include <vengine/drawing/Shader.hpp>

namespace vengine {
namespace drawing {
PipelineBuilder &PipelineBuilder::AddShaderStage(const Managed<Shader> &shader) {
  _shaders.push(shader);
  return *this;
}

PipelineBuilder &PipelineBuilder::SetNumViewports(const uint32_t num) {

  _numViewports = num;

  return *this;
}

PipelineBuilder &PipelineBuilder::SetNumScissors(const uint32_t num) {
  _numScissors = num;

  return *this;
}


PipelineBuilder &PipelineBuilder::VertexInput(
    const Array<vk::VertexInputBindingDescription>
    &bindings, const Array<vk::VertexInputAttributeDescription> &attributes) {
  _vertexInputInfo = vk::PipelineVertexInputStateCreateInfo(
      {}, bindings, attributes);
  return *this;
}

PipelineBuilder &
PipelineBuilder::SetInputTopology(const vk::PrimitiveTopology topology) {
  _inputAssembly.setTopology(topology);
  _inputAssembly.setPrimitiveRestartEnable(false);
  return *this;
}

PipelineBuilder &PipelineBuilder::SetPolygonMode(
    vk::PolygonMode polygonMode) {
  _rasterizer.setPolygonMode(polygonMode);
  _rasterizer.setLineWidth(1.0f);
  return *this;
}

PipelineBuilder &PipelineBuilder::SetCullMode(const vk::CullModeFlags cullMode,
                                              const vk::FrontFace frontFace) {
  _rasterizer.setCullMode(cullMode);
  _rasterizer.setFrontFace(frontFace);
  return *this;
}

PipelineBuilder &PipelineBuilder::SetMultisamplingModeNone() {
  _multisampling.setSampleShadingEnable(false);
  _multisampling.setRasterizationSamples(vk::SampleCountFlagBits::e1);
  _multisampling.setMinSampleShading(1.0f);
  _multisampling.setPSampleMask(nullptr);
  _multisampling.setAlphaToCoverageEnable(false);
  _multisampling.setAlphaToOneEnable(false);
  return *this;
}

PipelineBuilder &PipelineBuilder::DisableBlending() {
  _colorBlendAttachment.setColorWriteMask(
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  _colorBlendAttachment.setBlendEnable(false);

  return *this;
}

PipelineBuilder &PipelineBuilder::EnableBlendingAdditive() {
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

PipelineBuilder &PipelineBuilder::EnableBlendingAlphaBlend() {
  _colorBlendAttachment.setColorWriteMask(
      vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
      vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
  _colorBlendAttachment.setBlendEnable(true);
  _colorBlendAttachment.setSrcColorBlendFactor(vk::BlendFactor::eSrcAlpha);
  _colorBlendAttachment.setDstColorBlendFactor(vk::BlendFactor::eOneMinusSrcAlpha);
  _colorBlendAttachment.setColorBlendOp(vk::BlendOp::eAdd);
  
  _colorBlendAttachment.setSrcAlphaBlendFactor(vk::BlendFactor::eSrcAlpha);
  _colorBlendAttachment.setDstAlphaBlendFactor(vk::BlendFactor::eOneMinusSrcAlpha);
  _colorBlendAttachment.setAlphaBlendOp(vk::BlendOp::eSubtract);
  
  return *this;
}

PipelineBuilder &PipelineBuilder::SetColorAttachmentFormat(const vk::Format format) {
  _colorAttachmentFormat = format;
  return *this;
}

PipelineBuilder &PipelineBuilder::SetDepthFormat(const vk::Format format) {
  _renderInfo.setDepthAttachmentFormat(format);
  _renderInfo.setColorAttachmentFormats(_colorAttachmentFormat);
  return *this;
}

PipelineBuilder &PipelineBuilder::EnableDepthTest(bool depthWriteEnable,
                                                  const vk::CompareOp op) {
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

PipelineBuilder &PipelineBuilder::DisableDepthTest() {
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

PipelineBuilder &PipelineBuilder::SetLayout(const vk::PipelineLayout layout) {
  _layout = layout;

  return *this;
}

vk::Pipeline PipelineBuilder::Build(const vk::Device device) {

  Array<vk::PipelineShaderStageCreateInfo> _shaderStages{};

  for(const auto &shader : _shaders) {
    auto info = vk::PipelineShaderStageCreateInfo(
      vk::PipelineShaderStageCreateFlags(), shader->GetStage(), *shader.Get(), "main");
    _shaderStages.push(info);
  }
  
  const auto viewportCreateInfo = vk::PipelineViewportStateCreateInfo(
      vk::PipelineViewportStateCreateFlags(), _numViewports, nullptr,
      _numScissors, nullptr);

  const auto colorBlending = vk::PipelineColorBlendStateCreateInfo(
      vk::PipelineColorBlendStateCreateFlags(), vk::False, vk::LogicOp::eCopy,
      _colorBlendAttachment);

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
    throw std::runtime_error("Failed to create pipeline");
  }

  return result.value;
}

}
}
