#pragma once
#include "PipelineBuilder.hpp"
#include "types.hpp"

namespace vengine::drawing {
class MaterialInstance;
class DrawingSubsystem;
class MaterialBuilder {
  EMaterialType _type;
  PipelineBuilder _pipelineBuilder;
  DescriptorLayoutBuilder _layoutBuilder;
  Array<Managed<Shader>> _shaders;
  std::unordered_map<std::string,uint32_t> _pushConstants;
  friend class MaterialInstance;
  
protected:
  static Array<vk::PushConstantRange> ComputePushConstantRanges(ShaderResources& resources);
public:
  
  virtual MaterialBuilder& AddShader(Managed<Shader> shader);
  virtual MaterialBuilder& SetType(EMaterialType type);
  template<typename T>
  MaterialBuilder& ConfigurePushConstant(String name);
  
  virtual Managed<MaterialInstance> Create(DrawingSubsystem * drawer);

};

template <typename T> MaterialBuilder & MaterialBuilder::ConfigurePushConstant(String name) {
  _pushConstants.emplace(name,static_cast<uint32_t>(sizeof(T)));
  return *this;
}


}
