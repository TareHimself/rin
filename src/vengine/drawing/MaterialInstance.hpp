#pragma once
#include "Drawer.hpp"
#include "PipelineBuilder.hpp"
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
  vk::DescriptorSet _materialSet;
  vk::DescriptorSetLayout _materialSetLayout;
  EMaterialPass _passType;
  std::unordered_map<std::string,MaterialResourceInfo> _shaderResources;

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
  void SetSet(vk::DescriptorSet set);
  void SetSetLayout(vk::DescriptorSetLayout setLayout);
  void SetPass(EMaterialPass pass);
  void SetResources(const std::unordered_map<std::string,MaterialResourceInfo> &resources);

  vk::Pipeline GetPipeline() const;
  vk::PipelineLayout GetLayout() const;
  vk::DescriptorSet GetDescriptorSet() const;
  vk::DescriptorSetLayout GetDescriptorSetLayout() const;
  std::unordered_map<std::string,MaterialResourceInfo> GetResources() const;
  EMaterialPass GetPass() const;
  
  void Init(Drawer *drawer) override;

  MaterialInstance * SetTexture(const std::string &param, Texture * texture);

  
  void Bind(const SceneFrameData * frame) const;

  void PushConstant(const vk::CommandBuffer * cmd,const std::string &param,size_t size, const void * data);

  void HandleDestroy() override;
};

}
