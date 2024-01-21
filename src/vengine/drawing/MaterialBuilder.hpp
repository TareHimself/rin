#pragma once
#include "PipelineBuilder.hpp"
#include "types.hpp"

namespace vengine::drawing {
class Drawer;
class MaterialBuilder {
  EMaterialPass _pass;
  PipelineBuilder _pipelineBuilder;
  DescriptorLayoutBuilder _layoutBuilder;
  std::unordered_map<std::string,MaterialResourceInfo> _shaderResources;
  
public:

  friend class MaterialInstance;
  
  MaterialBuilder& AddShader(const Shader * shader,vk::ShaderStageFlagBits bits);
  MaterialBuilder& SetPass(EMaterialPass pass);
  MaterialInstance * Create(Drawer * drawer);
};


}
