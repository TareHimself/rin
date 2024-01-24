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
  vk::PipelineLayout _layout;
  Array<vk::DescriptorSet> _sets;
  Array<vk::DescriptorSetLayout> _setLayouts;
  // vk::DescriptorSetLayout _materialSetLayout;
  EMaterialPass _passType;
  uint64_t numDescriptorBindings = 0;
  ShaderResources _shaderResources;




protected:

  
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
  void SetSets(const Array<vk::DescriptorSet> &sets,const Array<vk::DescriptorSetLayout> &layouts);
  //void SetSetLayout(vk::DescriptorSetLayout setLayout);
  void SetPass(EMaterialPass pass);
  void SetResources(const ShaderResources &resources);
  void SetNumDescriptorBindings(uint64_t bindings);

  vk::Pipeline GetPipeline() const;
  vk::PipelineLayout GetLayout() const;
  Array<vk::DescriptorSet> GetDescriptorSets() const;
  //vk::DescriptorSetLayout GetDescriptorSetLayout() const;
  ShaderResources GetResources() const;
  EMaterialPass GetPass() const;
  
  void Init(Drawer *drawer) override;

  void SetTexture(const std::string &param, Texture * texture);

  template<typename T>
  void SetBuffer(const std::string &param, const AllocatedBuffer &buffer);

  void SetBuffer(const std::string &param, const AllocatedBuffer &buffer,size_t);

  void BindPipeline(RawFrameData *frame) const;
  //void BindCustomSet(RawFrameData *frame, const vk::DescriptorSet set, const uint32_t idx) const;
  void BindSets(RawFrameData *frame) const;
  //void Bind(const SceneFrameData * frame) const;

  //void Bind(RawFrameData *frame, uint32_t materialSetIndex = 0) const;

  template <typename T>
  void PushConstant(const vk::CommandBuffer * cmd,const std::string &param,const T &data);

  void HandleDestroy() override;
};

template <typename T> void MaterialInstance::SetBuffer(const std::string &param,
    const AllocatedBuffer &buffer) {
  SetBuffer(param,buffer,sizeof(T));
}

template <typename T> void MaterialInstance::PushConstant(
    const vk::CommandBuffer *cmd, const std::string &param,
    const T &data) {
  utils::vassert(_shaderResources.pushConstants.contains(param),"PushConstant [ {} ] does not exist in material",param);
  utils::vassert(sizeof(T) == _shaderResources.pushConstants[param].size,"PushConstant [ {} ] size mismatch",param);
  cmd->pushConstants(_layout,_shaderResources.pushConstants[param].stages,_shaderResources.pushConstants[param].offset,_shaderResources.pushConstants[param].size,&data);
}

}
