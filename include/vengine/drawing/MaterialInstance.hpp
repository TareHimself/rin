#pragma once
#include "Drawer.hpp"
#include "vengine/utils.hpp"
#include "vengine/Object.hpp"

namespace vengine::drawing {
class Texture;
}

namespace vengine::drawing {
struct SceneFrameData;
}

namespace vengine::drawing {

class MaterialInstance : public Object<Drawer> {
  vk::Pipeline _pipeline;
  vk::PipelineLayout _pipelineLayout;
  std::unordered_map<RawFrameData *,WeakPointer<DescriptorSet>> _dynamicSets;
  std::unordered_map<uint64_t,RawFrameData> _pendingCleanups;
  std::unordered_map<EMaterialSetType,WeakPointer<DescriptorSet>> _sets;
  std::unordered_map<EMaterialSetType,vk::DescriptorSetLayout> _layouts;
  // vk::DescriptorSetLayout _materialSetLayout;
  EMaterialType _materialType{};
  ShaderResources _shaderResources;

  
public:

  struct MaterialResources {
    AllocatedImage color;
    vk::Sampler colorSampler;
    AllocatedImage metallic;
    vk::Sampler metallicSampler;
    vk::Buffer dataBuffer;
    uint32_t dataBufferOffset;
  }; 

  void SetPipeline(vk::Pipeline pipeline);
  void SetLayout(vk::PipelineLayout layout);
  void SetSets(const std::unordered_map<EMaterialSetType, WeakPointer<DescriptorSet>> &
               sets, const std::unordered_map<EMaterialSetType, vk::DescriptorSetLayout> &
               layouts);
  void SetType(EMaterialType pass);
  void SetResources(const ShaderResources &resources);
  
  vk::Pipeline GetPipeline() const;
  vk::PipelineLayout GetLayout() const;
  std::unordered_map<EMaterialSetType, WeakPointer<DescriptorSet>>
  GetDescriptorSets() const;
  //vk::DescriptorSetLayout GetDescriptorSetLayout() const;
  ShaderResources GetResources() const;
  EMaterialType GetPass() const;
  
  void Init(Drawer * drawer) override;

  void SetTexture(const std::string &param, const WeakPointer<Texture> &texture);

  void SetDynamicTexture(RawFrameData *frame, const std::string &param, const WeakPointer<Texture> &
                         texture);

  void SetTextureArray(const std::string &param, const Array<WeakPointer<Texture>> &textures);

  template<typename T>
  void SetBuffer(const std::string &param, const WeakPointer<AllocatedBuffer> &buffer);

  void SetBuffer(const std::string &param, const WeakPointer<AllocatedBuffer> &buffer, size_t);

  void BindPipeline(RawFrameData *frame) const;
  //void BindCustomSet(RawFrameData *frame, const vk::DescriptorSet set, const uint32_t idx) const;
  void BindSets(RawFrameData *frame);

  void AllocateDynamicSet(RawFrameData *frame);
  //void Bind(const SceneFrameData * frame) const;

  //void Bind(RawFrameData *frame, uint32_t materialSetIndex = 0) const;

  template <typename T>
  void PushConstant(const vk::CommandBuffer * cmd,const std::string &param,const T &data);

  void HandleDestroy() override;
};

template <typename T> void MaterialInstance::SetBuffer(const std::string &param,
    const WeakPointer<AllocatedBuffer> &buffer) {
  SetBuffer(param,buffer,sizeof(T));
}

template <typename T> void MaterialInstance::PushConstant(
    const vk::CommandBuffer *cmd, const std::string &param,
    const T &data) {
  utils::vassert(_shaderResources.pushConstants.contains(param),"PushConstant [ {} ] does not exist in material",param);
  utils::vassert(sizeof(T) == _shaderResources.pushConstants[param].size,"PushConstant [ {} ] size mismatch",param);
  cmd->pushConstants(_pipelineLayout,_shaderResources.pushConstants[param].stages,_shaderResources.pushConstants[param].offset,_shaderResources.pushConstants[param].size,&data);
}

}
