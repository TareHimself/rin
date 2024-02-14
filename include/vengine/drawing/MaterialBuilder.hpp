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
  friend class MaterialInstance;
  
protected:
  static Array<vk::PushConstantRange> ComputePushConstantRanges(ShaderResources& resources);
public:
  
  virtual MaterialBuilder& AddShader(Managed<Shader> shader);
  
  virtual MaterialBuilder& SetType(EMaterialType type);
  
  virtual Managed<MaterialInstance> Create();

};


}
