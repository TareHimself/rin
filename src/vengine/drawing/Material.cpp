#include "Material.hpp"

#include "PipelineBuilder.hpp"
#include "scene/types.hpp"
#include "vengine/io/io.hpp"

namespace vengine {
namespace drawing {

void Material::init(Drawer *drawer) {
  Object<Drawer>::init(drawer);
  
  
}

void Material::init(Drawer *drawer, EMaterialPass pass,
    DescriptorAllocatorGrowable allocator, const MaterialResources &resources) {
  _passType = pass;
  const auto fragShader = Shader::fromSource(drawer->getShaderManager(), io::getRawShaderPath("mesh.frag"));
  const auto vertexShader = Shader::fromSource(drawer->getShaderManager(), io::getRawShaderPath("mesh.vert"));

  vk::PushConstantRange matrixRange = {vk::ShaderStageFlagBits::eVertex,0,sizeof(GpuDrawPushConstants)};
  DescriptorLayoutBuilder layoutBuilder;
  layoutBuilder.addBinding(0,vk::DescriptorType::eUniformBuffer);
  layoutBuilder.addBinding(1,vk::DescriptorType::eCombinedImageSampler);
  layoutBuilder.addBinding(2,vk::DescriptorType::eCombinedImageSampler);

  _materialSetLayout = layoutBuilder.build(drawer->getDevice(),vk::ShaderStageFlagBits::eVertex | vk::ShaderStageFlagBits::eFragment);

  vk::DescriptorSetLayout layouts[] = {drawer->getSceneDescriptorLayout(),_materialSetLayout};

  const auto meshLayoutInfo = vk::PipelineLayoutCreateInfo({},layouts,matrixRange);

  _layout = drawer->getDevice().createPipelineLayout(meshLayoutInfo);

  PipelineBuilder pipelineBuilder;
  pipelineBuilder
  .addVertexShader(vertexShader)
  .addFragmentShader(fragShader)
  .setInputTopology(vk::PrimitiveTopology::eTriangleList)
  .setPolygonMode(vk::PolygonMode::eFill)
  .setCullMode(vk::CullModeFlagBits::eNone,vk::FrontFace::eClockwise)
  .setMultisamplingModeNone()
  .disableBlending()
  .enableDepthTest(true,vk::CompareOp::eLessOrEqual)
  .setColorAttachmentFormat(drawer->getDrawImageFormat())
  .setDepthFormat(drawer->getDepthImageFormat())
  .setLayout(_layout);


  if(_passType == EMaterialPass::Transparent) {
    pipelineBuilder
    .enableBlendingAdditive()
    .enableDepthTest(false,vk::CompareOp::eLessOrEqual);
  }

  _pipeline = pipelineBuilder.build(drawer->getDevice());

  _materialSet = allocator.allocate(_materialSetLayout);

  DescriptorWriter writer;
  
  writer.writeBuffer(0,resources.dataBuffer,sizeof(MaterialConstants),resources.dataBufferOffset,vk::DescriptorType::eUniformBuffer);
  writer.writeImage(1,resources.color.view,resources.colorSampler,vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
  writer.writeImage(2,resources.metallic.view,resources.metallicSampler,vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);

  writer.updateSet(drawer->getDevice(),_materialSet);
  
  init(drawer);
}

Material * Material::create(Drawer *drawer, EMaterialPass pass,
                                    const DescriptorAllocatorGrowable &allocator, const MaterialResources &resources) {
  const auto material  = newObject<Material>();
  material->init(drawer,pass,allocator,resources);
  return material;
}

void Material::bind(const SceneFrameData * frame) const {
  const auto cmd = frame->getCmd();
  cmd->bindPipeline(vk::PipelineBindPoint::eGraphics,_pipeline);
  const auto sceneDescriptor = frame->getSceneDescriptor();
  cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,0,sceneDescriptor,{});
  cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,1,_materialSet,{});
}

void Material::pushVertexConstant(const SceneFrameData *frame, size_t size,
                                  const void *data) const {
  const auto cmd = frame->getCmd();
  cmd->pushConstants(_layout,vk::ShaderStageFlagBits::eVertex,0,size,data);
}

}
}
