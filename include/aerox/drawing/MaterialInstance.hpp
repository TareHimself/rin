#pragma once
#include "DrawingSubsystem.hpp"
#include "aerox/utils.hpp"
#include "aerox/Object.hpp"
#include "gen/drawing/MaterialInstance.gen.hpp"

namespace aerox::drawing {
class Texture;
}

namespace aerox::drawing {
struct SceneFrameData;
}

namespace aerox::drawing {

META_TYPE()
class MaterialInstance : public Object {
  vk::Pipeline _pipeline;
  vk::PipelineLayout _pipelineLayout;
  std::unordered_map<RawFrameData *,std::weak_ptr<DescriptorSet>> _dynamicSets;
  std::unordered_map<uint64_t,RawFrameData> _pendingCleanups;
  std::unordered_map<EMaterialSetType,std::weak_ptr<DescriptorSet>> _sets;
  std::unordered_map<EMaterialSetType,vk::DescriptorSetLayout> _layouts;
  // vk::DescriptorSetLayout _materialSetLayout;
  EMaterialType _materialType{};
  ShaderResources _shaderResources;

  
public:

  META_BODY()
  
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
  void SetSets(const std::unordered_map<EMaterialSetType, std::weak_ptr<DescriptorSet>> &
               sets, const std::unordered_map<EMaterialSetType, vk::DescriptorSetLayout> &
               layouts);
  void SetType(EMaterialType pass);
  void SetResources(const ShaderResources &resources);
  
  vk::Pipeline GetPipeline() const;
  vk::PipelineLayout GetLayout() const;
  std::unordered_map<EMaterialSetType, std::weak_ptr<DescriptorSet>>
  GetDescriptorSets() const;
  //vk::DescriptorSetLayout GetDescriptorSetLayout() const;
  ShaderResources GetResources() const;
  EMaterialType GetPass() const;
  
  void SetImage(const std::string &param, const std::shared_ptr<AllocatedImage> &image, const vk::
                Sampler &sampler);
  
  void SetTexture(const std::string &param, const std::shared_ptr<Texture> &texture);

  void SetDynamicTexture(RawFrameData *frame, const std::string &param, const std::shared_ptr<Texture> &texture);

  void SetTextureArray(const std::string &param, const Array<std::shared_ptr<Texture>> &textures);
  
  void SetBuffer(const std::string &param, const std::shared_ptr<AllocatedBuffer> &buffer, uint32_t
                 offset = 0);

  void BindPipeline(RawFrameData *frame) const;
  //void BindCustomSet(RawFrameData *frame, const vk::DescriptorSet set, const uint32_t idx) const;
  void BindSets(RawFrameData *frame);

  void AllocateDynamicSet(RawFrameData *frame);
  //void Bind(const SceneFrameData * frame) const;

  //void Bind(RawFrameData *frame, uint32_t materialSetIndex = 0) const;

  template <typename T>
  void Push(const vk::CommandBuffer * cmd,const std::string &param,const T &data);

  void OnDestroy() override;
};


template <typename T> void MaterialInstance::Push(
    const vk::CommandBuffer *cmd, const std::string &param,
    const T &data) {
  utils::vassert(_shaderResources.pushConstants.contains(param),"PushConstant [ {} ] does not exist in material",param);
  const auto dataSize = sizeof(T);
  const auto constSize = _shaderResources.pushConstants[param].size;
  utils::vassert(dataSize == constSize,"PushConstant [ {} ] size mismatch. Expected {} got {}.",param,constSize,dataSize);
  cmd->pushConstants(_pipelineLayout,_shaderResources.pushConstants[param].stages,_shaderResources.pushConstants[param].offset,_shaderResources.pushConstants[param].size,&data);
}
}
