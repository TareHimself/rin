#pragma once
#include "PipelineBuilder.hpp"
#include "types.hpp"

namespace vengine::drawing {
class MaterialInstance;
class Drawer;
class MaterialBuilder {
  EMaterialPass _pass;
  PipelineBuilder _pipelineBuilder;
  DescriptorLayoutBuilder _layoutBuilder;
  Array<Shader *> _shaders;
  std::unordered_map<std::string,uint32_t> _pushConstants;
  friend class MaterialInstance;
  
protected:
  static Array<vk::PushConstantRange> ComputePushConstantRanges(ShaderResources& resources);
  ShaderResources ComputeResources();
public:
  
  virtual MaterialBuilder& AddShader(Shader * shader);
  virtual MaterialBuilder& SetPass(EMaterialPass pass);
  template<typename T>
  MaterialBuilder& ConfigurePushConstant(String name);
  
  virtual MaterialInstance * Create(Drawer * drawer);

  ~MaterialBuilder();
};

template <typename T> MaterialBuilder & MaterialBuilder::ConfigurePushConstant(String name) {
  _pushConstants.emplace(name,static_cast<uint32_t>(sizeof(T)));
  return *this;
}


}
