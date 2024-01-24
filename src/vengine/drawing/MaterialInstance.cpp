#include "MaterialInstance.hpp"

#include "PipelineBuilder.hpp"
#include "Texture.hpp"
#include "scene/types.hpp"
#include "vengine/utils.hpp"

namespace vengine::drawing {

void MaterialInstance::SetPipeline(const vk::Pipeline pipeline) {
  _pipeline = pipeline;
}

void MaterialInstance::SetLayout(const vk::PipelineLayout layout) {
  _layout = layout;
}

void MaterialInstance::SetSets(const Array<vk::DescriptorSet> &sets,
    const Array<vk::DescriptorSetLayout> &layouts) {
  _sets = sets;
  _setLayouts = layouts;
}

// void MaterialInstance::SetSetLayout(const vk::DescriptorSetLayout setLayout) {
//   _materialSetLayout = setLayout;
// }

void MaterialInstance::SetPass(const EMaterialPass pass) {
  _passType = pass;
}

void MaterialInstance::SetResources(
    const ShaderResources &resources) {
  _shaderResources = resources;
}

void MaterialInstance::SetNumDescriptorBindings(const uint64_t bindings) {
  numDescriptorBindings = bindings;
}

vk::Pipeline MaterialInstance::GetPipeline() const {
  return _pipeline;
}

vk::PipelineLayout MaterialInstance::GetLayout() const {
  return _layout;
}

Array<vk::DescriptorSet> MaterialInstance::GetDescriptorSets() const {
  return _sets;
}


// vk::DescriptorSetLayout MaterialInstance::GetDescriptorSetLayout() const {
//   return _materialSetLayout;
// }

ShaderResources MaterialInstance::GetResources() const {
  return _shaderResources;
}

EMaterialPass MaterialInstance::GetPass() const {
  return _passType;
}

void MaterialInstance::Init(Drawer *drawer) {
  Object<Drawer>::Init(drawer);
  
  
}

void MaterialInstance::SetTexture(const std::string &param,
                                                Texture *texture) {


  utils::vassert(_shaderResources.images.contains(param),"Texture [ {} ] Does Not Exist In Material",param);
  utils::vassert(texture != nullptr,"Texture Is Invalid");

  if(!texture->IsUploaded()) {
    texture->Upload();
  }
  
  DescriptorWriter writer;
  const auto imageInfo = _shaderResources.images[param];
  writer.WriteImage(imageInfo.binding,texture->GetGpuData().value().view,texture->GetSampler(),vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
  writer.UpdateSet(GetOuter()->GetDevice(),_sets[imageInfo.set]);
}

void MaterialInstance::SetBuffer(const std::string &param,
    const AllocatedBuffer &buffer, size_t size) {

  utils::vassert(_shaderResources.uniformBuffers.contains(param),"UniformBuffer [ {} ] Does Not Exist In Material",param);

  const auto bufferInfo = _shaderResources.uniformBuffers[param];
  
  DescriptorWriter writer;
  
  writer.WriteBuffer(bufferInfo.binding, buffer.buffer, size, 0,vk::DescriptorType::eUniformBuffer);
  writer.UpdateSet(GetOuter()->GetDevice(),_sets[bufferInfo.set]);
}

void MaterialInstance::BindPipeline(RawFrameData *frame) const {
  const auto cmd = frame->GetCmd();
  cmd->bindPipeline(vk::PipelineBindPoint::eGraphics,_pipeline);
}

void MaterialInstance::BindSets(RawFrameData *frame) const {
  const auto cmd = frame->GetCmd();
  if(!_sets.empty()) {
    for(auto i = 0; i < _sets.size(); i++) {
      cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,i,_sets[i],{});
    }
  }
}

// void MaterialInstance::Bind(const SceneFrameData * frame) const {
//   const auto cmd = frame->GetCmd();
//   Bind(frame->GetDrawerFrameData(),1);
//   const auto sceneDescriptor = frame->GetSceneDescriptor();
//   cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,0,sceneDescriptor,{});
// }

void MaterialInstance::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  const auto device = GetOuter()->GetDevice();
  device.waitIdle();
  device.destroyPipeline(_pipeline);
  device.destroyPipelineLayout(_layout);
  for(const auto layout : _setLayouts) {
    device.destroyDescriptorSetLayout(layout);
  }
  
}

}
