#pragma once
#include "PipelineBuilder.hpp"
#include "types.hpp"

namespace aerox::drawing {
class MaterialInstance;
class DrawingSubsystem;
class MaterialBuilder {
  EMaterialType _type;
  PipelineBuilder _pipelineBuilder;
  DescriptorLayoutBuilder _layoutBuilder;
  Array<std::shared_ptr<Shader>> _shaders;
  friend class MaterialInstance;
  
protected:
  static Array<vk::PushConstantRange> ComputePushConstantRanges(ShaderResources& resources);
public:
  
  virtual MaterialBuilder& AddShader(std::shared_ptr<Shader> shader);

  virtual MaterialBuilder& AddShaders(const Array<std::shared_ptr<Shader>> &shaders);

  virtual MaterialBuilder& AddAttachmentFormats(const Array<vk::Format> &formats);
  
  virtual MaterialBuilder& SetType(EMaterialType type);
  
  virtual std::shared_ptr<MaterialInstance> Create();

};


}
