#include "MaterialBuilder.hpp"

#include "Drawer.hpp"
#include "MaterialInstance.hpp"
#include "vengine/utils.hpp"

namespace vengine::drawing {
MaterialBuilder& MaterialBuilder::AddShader(Shader *shader) {
  shader->Use();
  _pipelineBuilder.AddShaderStage(shader);
  _shaders.Push(shader);
  return *this;
}

MaterialBuilder& MaterialBuilder::SetPass(const EMaterialPass pass) {
  _pass = pass;
  return *this;
}

Array<vk::PushConstantRange> MaterialBuilder::ComputePushConstantRanges(
    ShaderResources& resources) {
  Array<vk::PushConstantRange> ranges;
  uint32_t offset = 0;
  vk::ShaderStageFlags flags{};
  for(auto &info : resources.pushConstants | std::views::values) {
    
    ranges.Push({info.stages,offset,info.size});
    flags |= info.stages;
    info.offset = offset;
    offset += info.size;
  }

  return ranges;
}

ShaderResources MaterialBuilder::ComputeResources() {
  ShaderResources resources{};
  
  
  for(auto shader : _shaders) {
    auto shaderResources = shader->GetResources();
    auto shaderStage = shader->GetStage();
    for(auto &val : shaderResources.images) {
      resources.images.insert(val);
    }

    for(auto &val : shaderResources.uniformBuffers) {
      resources.uniformBuffers.insert(val);
    }

    for(const auto &key : shaderResources.pushConstants | std::views::keys) {
      utils::vassert(_pushConstants.contains(key),"Missing push constant [ {} ] for shader {}",key,shader->GetSourcePath().string());
      if(!resources.pushConstants.contains(key)) {
        resources.pushConstants.emplace(key,PushConstantInfo{_pushConstants[key],shaderStage});
      }
      else {
        auto existing = resources.pushConstants[key];
        existing.stages |= shaderStage;
        resources.pushConstants.emplace(key,existing);
      }
    }
  }

  return resources;
}

MaterialInstance * MaterialBuilder::Create(Drawer * drawer) {
  auto device = drawer->GetDevice();
  const auto instance = newObject<MaterialInstance>();
  ShaderResources resources{};
  
  Array<DescriptorLayoutBuilder> layoutBuilders;
  
  for(auto shader : _shaders) {
    auto shaderResources = shader->GetResources();
    auto shaderStage = shader->GetStage();
    for(auto &val : shaderResources.images) {
      resources.images.insert(val);
      
      if(layoutBuilders.size() < (val.second.set + 1)) {
        layoutBuilders.resize(val.second.set + 1);
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,vk::DescriptorType::eCombinedImageSampler,shaderStage);
    }

    for(auto &val : shaderResources.uniformBuffers) {
      resources.uniformBuffers.insert(val);

      if(layoutBuilders.size() < (val.second.set + 1)) {
        layoutBuilders.resize(val.second.set + 1);
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,vk::DescriptorType::eUniformBuffer,shaderStage);
    }

    for(const auto &key : shaderResources.pushConstants | std::views::keys) {
      utils::vassert(_pushConstants.contains(key),"Missing push constant [ {} ] for shader {}",key,shader->GetSourcePath().string());
      if(!resources.pushConstants.contains(key)) {
        resources.pushConstants.emplace(key,PushConstantInfo{_pushConstants[key],shaderStage});
      }
      else {
        auto existing = resources.pushConstants[key];
        existing.stages |= shaderStage;
        resources.pushConstants.emplace(key,existing);
      }
    }
  }
  
  auto pushConstants = ComputePushConstantRanges(resources);

  Array<vk::DescriptorSetLayout> layouts;
  auto numBindings = 0;
  for(auto i = 0; i <  layoutBuilders.size(); i++) {
    numBindings += layoutBuilders[i].bindings.size();
    layouts.Push(layoutBuilders[i].Build(device));
  }
  
  //instance->SetSetLayout(_layoutBuilder.Build(drawer->GetDevice()));
  instance->SetNumDescriptorBindings(numBindings);
  
  const auto uiLayoutInfo = vk::PipelineLayoutCreateInfo({},layouts,pushConstants);

  instance->SetLayout(drawer->GetDevice().createPipelineLayout(uiLayoutInfo));


  if(_pass == EMaterialPass::UI) {
    _pipelineBuilder
      .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
      .SetPolygonMode(vk::PolygonMode::eFill)
      .SetCullMode(vk::CullModeFlagBits::eNone,vk::FrontFace::eClockwise)
      .SetMultisamplingModeNone()
      .DisableBlending()
      .DisableDepthTest()
      .SetColorAttachmentFormat(drawer->GetDrawImageFormat())
      .SetLayout(instance->GetLayout());
  } else {
    _pipelineBuilder
      .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
      .SetPolygonMode(vk::PolygonMode::eFill)
      .SetCullMode(vk::CullModeFlagBits::eNone,vk::FrontFace::eClockwise)
      .SetMultisamplingModeNone()
      .DisableBlending()
      .EnableDepthTest(true,vk::CompareOp::eLessOrEqual)
      .SetColorAttachmentFormat(drawer->GetDrawImageFormat())
      .SetDepthFormat(drawer->GetDepthImageFormat())
      .SetLayout(instance->GetLayout());

    if(_pass == EMaterialPass::Transparent) {
      _pipelineBuilder
          .EnableBlendingAdditive()
          .EnableDepthTest(false,vk::CompareOp::eLessOrEqual);
    }
  }
  
  instance->SetPass(_pass);

  instance->SetPipeline(_pipelineBuilder.Build(drawer->GetDevice()));

  Array<vk::DescriptorSet> sets;

  auto globalAllocator = drawer->GetGlobalDescriptorAllocator();
  
  for(auto i = 0; i <  layouts.size(); i++) {
    sets.Push(globalAllocator->Allocate(layouts[i]));
  }
  
  instance->SetSets(sets,layouts);
  instance->SetResources(resources);
  instance->Init(drawer);
  
  return instance;
}

MaterialBuilder::~MaterialBuilder() {
  for(const auto shader : _shaders) {
    shader->Destroy();
  }

  _shaders.clear();
}
}
