#include "MaterialInstance.hpp"

#include "PipelineBuilder.hpp"
#include "Texture.hpp"
#include "scene/types.hpp"
#include "vengine/utils.hpp"
#include "vengine/io/io.hpp"

namespace vengine::drawing {

void MaterialInstance::SetPipeline(const vk::Pipeline pipeline) {
  _pipeline = pipeline;
}

void MaterialInstance::SetLayout(const vk::PipelineLayout layout) {
  _layout = layout;
}

void MaterialInstance::SetSet(const vk::DescriptorSet set) {
  _materialSet = set;
}

void MaterialInstance::SetSetLayout(const vk::DescriptorSetLayout setLayout) {
  _materialSetLayout = setLayout;
}

void MaterialInstance::SetPass(const EMaterialPass pass) {
  _passType = pass;
}

void MaterialInstance::SetResources(
    const std::unordered_map<std::string, MaterialResourceInfo> &resources) {
  _shaderResources = resources;
}

vk::Pipeline MaterialInstance::GetPipeline() const {
  return _pipeline;
}

vk::PipelineLayout MaterialInstance::GetLayout() const {
  return _layout;
}

vk::DescriptorSet MaterialInstance::GetDescriptorSet() const {
  return _materialSet;
}

vk::DescriptorSetLayout MaterialInstance::GetDescriptorSetLayout() const {
  return _materialSetLayout;
}

std::unordered_map<std::string, MaterialResourceInfo> MaterialInstance::GetResources() const {
  return _shaderResources;
}

EMaterialPass MaterialInstance::GetPass() const {
  return _passType;
}

void MaterialInstance::Init(Drawer *drawer) {
  Object<Drawer>::Init(drawer);
  
  
}

MaterialInstance * MaterialInstance::SetTexture(const std::string &param,
                                                Texture *texture) {


  utils::vassert(_shaderResources.contains(param),"Texture Parameter Does Not Exist In Material");
  utils::vassert(texture != nullptr,"Texture Is Invalid");

  if(!texture->IsUploaded()) {
    texture->Upload();
  }
  
  DescriptorWriter writer;
  
  writer.WriteImage(_shaderResources[param].binding,texture->GetGpuData().value().view,texture->GetSampler(),vk::ImageLayout::eShaderReadOnlyOptimal,vk::DescriptorType::eCombinedImageSampler);
  writer.UpdateSet(GetOuter()->GetDevice(),_materialSet);
  return this;
}

void MaterialInstance::Bind(const SceneFrameData * frame) const {
  const auto cmd = frame->GetCmd();
  cmd->bindPipeline(vk::PipelineBindPoint::eGraphics,_pipeline);
  const auto sceneDescriptor = frame->GetSceneDescriptor();
  cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,0,sceneDescriptor,{});
  cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics,_layout,1,_materialSet,{});
}

void MaterialInstance::PushConstant(const vk::CommandBuffer * cmd,const std::string &param, const size_t size,
                                    const void *data) {
  utils::vassert(_shaderResources.contains(param),"PushConstant Parameter Does Not Exist In Material");
  cmd->pushConstants(_layout,_shaderResources[param].stages,0,size,data);
}

void MaterialInstance::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  const auto device = GetOuter()->GetDevice();
  device.waitIdle();
  device.destroyPipeline(_pipeline);
  device.destroyPipelineLayout(_layout);
  device.destroyDescriptorSetLayout(_materialSetLayout);
}

}
