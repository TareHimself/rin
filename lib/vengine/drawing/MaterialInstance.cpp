#include <vengine/drawing/MaterialInstance.hpp>
#include <vengine/drawing/PipelineBuilder.hpp>
#include <vengine/drawing/Texture2D.hpp>
#include "vengine/utils.hpp"

namespace vengine::drawing {

void MaterialInstance::SetPipeline(const vk::Pipeline pipeline) {
  _pipeline = pipeline;
}

void MaterialInstance::SetLayout(const vk::PipelineLayout layout) {
  _pipelineLayout = layout;
}

void MaterialInstance::SetSets(const std::unordered_map<EMaterialSetType, Ref<DescriptorSet>> &
                               sets, const std::unordered_map<EMaterialSetType, vk::DescriptorSetLayout> &
                               layouts) {
  _sets = sets;
  _layouts = layouts;
}

// void MaterialInstance::SetSetLayout(const vk::DescriptorSetLayout setLayout) {
//   _materialSetLayout = setLayout;
// }

void MaterialInstance::SetType(const EMaterialType pass) {
  _materialType = pass;
}

void MaterialInstance::SetResources(
    const ShaderResources &resources) {
  _shaderResources = resources;
}


vk::Pipeline MaterialInstance::GetPipeline() const {
  return _pipeline;
}

vk::PipelineLayout MaterialInstance::GetLayout() const {
  return _pipelineLayout;
}

std::unordered_map<EMaterialSetType, Ref<DescriptorSet>> MaterialInstance::GetDescriptorSets() const {
  return _sets;
}

ShaderResources MaterialInstance::GetResources() const {
  return _shaderResources;
}

EMaterialType MaterialInstance::GetPass() const {
  return _materialType;
}

void MaterialInstance::Init(DrawingSubsystem * drawer) {
  Object<DrawingSubsystem>::Init(drawer);
}

void MaterialInstance::SetTexture(const std::string &param,
                                                const Ref<Texture2D> &texture) {


  utils::vassert(_shaderResources.images.contains(param),"Texture [ {} ] Does Not Exist In Material",param);
  
  utils::vassert(texture,"Texture Is Invalid");
  
  const auto imageInfo = _shaderResources.images[param];
  
  utils::vassert(imageInfo.set != EMaterialSetType::Dynamic,"This function does not support dynamic descriptor sets");
  
  _sets[imageInfo.set].Reserve()->WriteTexture(imageInfo.binding,texture,vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetDynamicTexture(RawFrameData *frame,
    const std::string &param, const Ref<Texture2D> &texture) {
  utils::vassert(_shaderResources.images.contains(param),"Texture [ {} ] Does Not Exist In Material",param);
  utils::vassert(texture,"Texture Is Invalid");
  
  const auto imageInfo = _shaderResources.images[param];

  utils::vassert(imageInfo.set == EMaterialSetType::Dynamic,"This function only supports dynamic descriptor sets");
  
  _dynamicSets[frame].Reserve()->WriteTexture(imageInfo.binding,texture,vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetTextureArray(const std::string &param,
                                       const Array<Ref<Texture2D>> &textures) {
  utils::vassert(_shaderResources.images.contains(param),"Texture [ {} ] Does Not Exist In Material",param);
  utils::vassert(!textures.empty(),"Texture Array is Empty");
  
  const auto imageInfo = _shaderResources.images[param];
  utils::vassert(imageInfo.set != EMaterialSetType::Dynamic,"This function does not support dynamic descriptor sets");

  _sets[imageInfo.set].Reserve()->WriteTextureArray(imageInfo.binding,textures,vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetBuffer(const std::string &param,
                                 const Ref<AllocatedBuffer> &buffer,uint32_t offset) {

  utils::vassert(_shaderResources.uniformBuffers.contains(param),"UniformBuffer [ {} ] Does Not Exist In Material",param);

  const auto bufferInfo = _shaderResources.uniformBuffers[param];
  utils::vassert(bufferInfo.set != EMaterialSetType::Dynamic,"This function does not support dynamic descriptor sets");

  _sets[bufferInfo.set].Reserve()->WriteBuffer(bufferInfo.binding, buffer, offset, vk::DescriptorType::eUniformBuffer);
}

void MaterialInstance::BindPipeline(RawFrameData *frame) const {
  const auto cmd = frame->GetCmd();
  cmd->bindPipeline(vk::PipelineBindPoint::eGraphics,_pipeline);
}

void MaterialInstance::BindSets(RawFrameData *frame){
  
  if(!_sets.empty()) {
    const auto cmd = frame->GetCmd();
    for(auto [fst, snd] : _sets) {
      cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_pipelineLayout,
                              static_cast<uint32_t>(fst), static_cast<vk::DescriptorSet>(*snd.Reserve().Get()),{});
    }
  }

  if(_layouts.contains(Dynamic)) {
    const auto cmd = frame->GetCmd();
    
    if(!_dynamicSets.contains(frame) || !_dynamicSets[frame]) {
      AllocateDynamicSet(frame);
    }
    
    cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_pipelineLayout,Dynamic,static_cast<vk::DescriptorSet>(*_dynamicSets[frame].Reserve().Get()),{});
  }
}

void MaterialInstance::AllocateDynamicSet(RawFrameData *frame) {
  _dynamicSets[frame] = frame->GetDescriptorAllocator()->Allocate(_layouts[EMaterialSetType::Dynamic]);
}

void MaterialInstance::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  GetOuter()->WaitDeviceIdle();
  const auto device = GetOuter()->GetVirtualDevice();
  device.destroyPipeline(_pipeline);
  device.destroyPipelineLayout(_pipelineLayout);
  for(const auto val : _layouts | std::views::values) {
    device.destroyDescriptorSetLayout(val);
  }
  _dynamicSets.clear();
  _sets.clear();
}

}
