#include "MaterialBuilder.hpp"

#include "Drawer.hpp"
#include "MaterialInstance.hpp"

namespace vengine::drawing {
MaterialBuilder& MaterialBuilder::AddShader(const Shader *shader,
                                            const vk::ShaderStageFlagBits bits) {
  _pipelineBuilder.AddShaderStage(shader,bits);
  auto [images, pushConstants, uniformBuffers] = shader->GetResources();
  
  for(const auto &[key, val] : images) {
    _layoutBuilder.AddBinding(val.second,vk::DescriptorType::eCombinedImageSampler);
    _shaderResources.insert({key,{EMaterialResourceType::Image,bits,val.second}});
  }
  
  for(const auto &[key,val] : uniformBuffers) {
    _layoutBuilder.AddBinding(val.second,vk::DescriptorType::eUniformBuffer);
    _shaderResources.insert({key,{EMaterialResourceType::UniformBuffer,bits,val.second}});
  }

  for(const auto &[key,val] : pushConstants) {
    _shaderResources.insert({key,{EMaterialResourceType::PushConstant,bits,val.second}});
  }
  
  return *this;
}

MaterialBuilder& MaterialBuilder::SetPass(const EMaterialPass pass) {
  _pass = pass;
  return *this;
}

MaterialInstance * MaterialBuilder::Create(Drawer * drawer) {

  const auto instance = newObject<MaterialInstance>();
  
  vk::PushConstantRange matrixRange = {vk::ShaderStageFlagBits::eVertex,0,sizeof(SceneDrawPushConstants)};

  instance->SetSetLayout(_layoutBuilder.Build(drawer->GetDevice(),vk::ShaderStageFlagBits::eVertex | vk::ShaderStageFlagBits::eFragment));
  
  vk::DescriptorSetLayout layouts[] = {drawer->GetSceneDescriptorLayout(),instance->GetDescriptorSetLayout()};

  const auto meshLayoutInfo = vk::PipelineLayoutCreateInfo({},layouts,matrixRange);

  instance->SetLayout(drawer->GetDevice().createPipelineLayout(meshLayoutInfo));
  
  _pipelineBuilder
      .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
      .SetPolygonMode(vk::PolygonMode::eFill)
      .SetCullMode(vk::CullModeFlagBits::eNone,vk::FrontFace::eClockwise)
      .SetMultisamplingModeNone()
      .DisableBlending()
      .EnableDepthTest(true,vk::CompareOp::eLessOrEqual)
      .SetColorAttachmentFormat(drawer->GetDrawImageFormat())
      .SetDepthFormat(drawer->GetDepthImageFormat())
      .SetLayout(instance->GetLayout());

  if(_pass == EMaterialPass::Transparent) {
    _pipelineBuilder
        .EnableBlendingAdditive()
        .EnableDepthTest(false,vk::CompareOp::eLessOrEqual);
  }

  instance->SetPass(_pass);

  instance->SetPipeline(_pipelineBuilder.Build(drawer->GetDevice()));

  instance->SetSet(drawer->GetGlobalDescriptorAllocator()->Allocate(instance->GetDescriptorSetLayout()));
  instance->SetResources(_shaderResources);
  instance->Init(drawer);
  
  return instance;
}
}
