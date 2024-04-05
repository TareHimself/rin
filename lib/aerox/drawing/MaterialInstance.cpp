#include "aerox/Engine.hpp"

#include <aerox/drawing/MaterialInstance.hpp>
#include <aerox/drawing/PipelineBuilder.hpp>
#include <aerox/drawing/Texture.hpp>
#include "aerox/utils.hpp"

namespace aerox::drawing {

void MaterialInstance::SetPipeline(const vk::Pipeline pipeline) {
  _pipeline = pipeline;
}

void MaterialInstance::SetLayout(const vk::PipelineLayout layout) {
  _pipelineLayout = layout;
}

void MaterialInstance::SetSets(
    const std::unordered_map<EMaterialSetType, std::weak_ptr<DescriptorSet>> &
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

std::unordered_map<EMaterialSetType, std::weak_ptr<DescriptorSet>>
MaterialInstance::GetDescriptorSets() const {
  return _sets;
}

ShaderResources MaterialInstance::GetResources() const {
  return _shaderResources;
}

EMaterialType MaterialInstance::GetPass() const {
  return _materialType;
}

void MaterialInstance::SetImage(const std::string &param,
                                const std::shared_ptr<AllocatedImage> &image,
                                const vk::Sampler &sampler) {
  utils::vassert(_shaderResources.images.contains(param),
                 "Texture [ {} ] Does Not Exist In Material", param);

  utils::vassert(static_cast<bool>(image), "Image Is Invalid");

  const auto imageInfo = _shaderResources.images[param];

  utils::vassert(imageInfo.set != EMaterialSetType::Dynamic,
                 "This function does not support dynamic descriptor sets");

  _sets[imageInfo.set].lock()->WriteImage(imageInfo.binding, image, sampler,
                                             vk::ImageLayout::eShaderReadOnlyOptimal,
                                             vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetTexture(const std::string &param,
                                  const std::shared_ptr<Texture> &texture) {

  utils::vassert(_shaderResources.images.contains(param),
                 "Texture [ {} ] Does Not Exist In Material", param);

  utils::vassert(static_cast<bool>(texture), "Texture Is Invalid");

  const auto imageInfo = _shaderResources.images[param];

  utils::vassert(imageInfo.set != EMaterialSetType::Dynamic,
                 "This function does not support dynamic descriptor sets");

  _sets[imageInfo.set].lock()->WriteTexture(imageInfo.binding, texture,
                                               vk::ImageLayout::eShaderReadOnlyOptimal,
                                               vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetDynamicTexture(RawFrameData *frame,
                                         const std::string &param,
                                         const std::shared_ptr<Texture> &texture) {
  utils::vassert(_shaderResources.images.contains(param),
                 "Texture [ {} ] Does Not Exist In Material", param);
  utils::vassert(static_cast<bool>(texture), "Texture Is Invalid");

  const auto imageInfo = _shaderResources.images[param];

  utils::vassert(imageInfo.set == EMaterialSetType::Dynamic,
                 "This function only supports dynamic descriptor sets");

  _dynamicSets[frame].lock()->WriteTexture(imageInfo.binding, texture,
                                              vk::ImageLayout::eShaderReadOnlyOptimal,
                                              vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetTextureArray(const std::string &param,
                                       const Array<std::shared_ptr<Texture>> &
                                       textures) {
  utils::vassert(_shaderResources.images.contains(param),
                 "Texture [ {} ] Does Not Exist In Material", param);
  utils::vassert(!textures.empty(), "Texture Array is Empty");

  const auto imageInfo = _shaderResources.images[param];
  utils::vassert(imageInfo.set != EMaterialSetType::Dynamic,
                 "This function does not support dynamic descriptor sets");

  _sets[imageInfo.set].lock()->WriteTextureArray(
      imageInfo.binding, textures, vk::ImageLayout::eShaderReadOnlyOptimal,
      vk::DescriptorType::eCombinedImageSampler);
}

void MaterialInstance::SetBuffer(const std::string &param,
                                 const std::shared_ptr<AllocatedBuffer> &buffer,
                                 uint32_t offset) {

  utils::vassert(_shaderResources.uniformBuffers.contains(param),
                 "UniformBuffer [ {} ] Does Not Exist In Material", param);

  const auto bufferInfo = _shaderResources.uniformBuffers[param];
  utils::vassert(bufferInfo.set != EMaterialSetType::Dynamic,
                 "This function does not support dynamic descriptor sets");

  _sets[bufferInfo.set].lock()->WriteBuffer(bufferInfo.binding, buffer,
                                               offset,
                                               vk::DescriptorType::eUniformBuffer);
}

void MaterialInstance::BindPipeline(RawFrameData *frame) const {
  const auto cmd = frame->GetCmd();
  cmd->bindPipeline(vk::PipelineBindPoint::eGraphics, _pipeline);
}

void MaterialInstance::BindSets(RawFrameData *frame) {

  if (!_sets.empty()) {
    const auto cmd = frame->GetCmd();
    for (auto [fst, snd] : _sets) {
      cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics, _pipelineLayout,
                              static_cast<uint32_t>(fst),
                              static_cast<vk::DescriptorSet>(*snd.lock().
                                get()), {});
    }
  }

  if (_layouts.contains(Dynamic)) {
    const auto cmd = frame->GetCmd();

    if (!_dynamicSets.contains(frame) || _dynamicSets[frame].expired()) {
      AllocateDynamicSet(frame);
    }

    cmd->bindDescriptorSets(vk::PipelineBindPoint::eGraphics, _pipelineLayout,
                            Dynamic,
                            static_cast<vk::DescriptorSet>(*_dynamicSets[frame].
                              lock().get()), {});
  }
}

void MaterialInstance::AllocateDynamicSet(RawFrameData *frame) {
  _dynamicSets[frame] = frame->GetDescriptorAllocator()->Allocate(
      _layouts[EMaterialSetType::Dynamic]);
}

void MaterialInstance::OnDestroy() {
  Object::OnDestroy();
  auto drawer = Engine::Get()->GetDrawingSubsystem().lock();
  drawer->WaitDeviceIdle();
  const auto device = drawer->GetVirtualDevice();
  device.destroyPipeline(_pipeline);
  device.destroyPipelineLayout(_pipelineLayout);
  for (const auto val : _layouts | std::views::values) {
    device.destroyDescriptorSetLayout(val);
  }
  _dynamicSets.clear();
  _sets.clear();
}

}
