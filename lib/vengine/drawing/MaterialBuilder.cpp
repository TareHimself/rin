#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/MaterialInstance.hpp>
#include "vengine/utils.hpp"

namespace vengine::drawing {
MaterialBuilder& MaterialBuilder::AddShader(Managed<Shader> shader) {
  _pipelineBuilder.AddShaderStage(shader);
  _shaders.push(shader);
  return *this;
}

MaterialBuilder& MaterialBuilder::SetType(const EMaterialType type) {
  _type = type;
  return *this;
}

Array<vk::PushConstantRange> MaterialBuilder::ComputePushConstantRanges(
    ShaderResources& resources) {
  Array<vk::PushConstantRange> ranges;
  uint32_t offset = 0;
  vk::ShaderStageFlags flags{};
  for(auto &info : resources.pushConstants | std::views::values) {
    
    ranges.push({info.stages,offset,info.size});
    flags |= info.stages;
    info.offset = offset;
    offset += info.size;
  }

  return ranges;
}

Managed<MaterialInstance> MaterialBuilder::Create(DrawingSubsystem * drawer) {
  auto device = drawer->GetDevice();
  const auto instance = newManagedObject<MaterialInstance>();
  ShaderResources resources{};

  std::unordered_map<EMaterialSetType,DescriptorLayoutBuilder> layoutBuilders;
  uint32_t maxLayout = 0;
  for(auto &shader : _shaders) {
    auto shaderResources = shader->GetResources();
    auto shaderStage = shader->GetStage();
    for(auto &val : shaderResources.images) {
      resources.images.insert(val);
      
      if(!layoutBuilders.contains(val.second.set)) {
        layoutBuilders.insert({val.second.set,{}});
        
        if(maxLayout < val.second.set) {
          maxLayout = val.second.set;
        }
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,vk::DescriptorType::eCombinedImageSampler,shaderStage,val.second.count);
    }

    for(auto &val : shaderResources.uniformBuffers) {
      resources.uniformBuffers.insert(val);

      if(!layoutBuilders.contains(val.second.set)) {
        layoutBuilders.insert({val.second.set,{}});

        if(maxLayout < val.second.set) {
          maxLayout = val.second.set;
        }
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,vk::DescriptorType::eUniformBuffer,shaderStage,val.second.count);
    }

    for(const auto &key : shaderResources.pushConstants | std::views::keys) {
      utils::vassert(_pushConstants.contains(key),"Missing push constant [ {} ] for shader {}",key,shader->GetSourcePath().string());
      if(!resources.pushConstants.contains(key)) {
        resources.pushConstants.emplace(key,PushConstantInfo{_pushConstants[key],shaderStage});
      }
      else {
        resources.pushConstants.find(key)->second.stages |= shaderStage;
      }
    }
  }
  
  auto pushConstants = ComputePushConstantRanges(resources);

  std::unordered_map<EMaterialSetType,vk::DescriptorSetLayout> layouts;
  Array<vk::DescriptorSetLayout> layoutsArr;
  for(auto i = 0; i < maxLayout + 1; i++) {
    auto layoutType = static_cast<EMaterialSetType>(i);
    auto layout = layoutBuilders.contains(layoutType) ? layoutBuilders[layoutType].Build(device) : DescriptorLayoutBuilder().Build(device);
    layouts.emplace(layoutType,layout);
    layoutsArr.push(layout);
  }
  //instance->SetSetLayout(_layoutBuilder.Build(drawer->GetDevice()));
  
  const auto pipelineLayoutInfo = vk::PipelineLayoutCreateInfo({},layoutsArr,pushConstants);

  instance->SetLayout(drawer->GetDevice().createPipelineLayout(pipelineLayoutInfo));


  if(_type == EMaterialType::UI) {
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

    if(_type == EMaterialType::Translucent) {
      _pipelineBuilder
          .EnableBlendingAdditive()
          .EnableDepthTest(false,vk::CompareOp::eLessOrEqual);
    }
  }
  
  instance->SetType(_type);

  instance->SetPipeline(_pipelineBuilder.Build(drawer->GetDevice()));

  auto globalAllocator = drawer->GetGlobalDescriptorAllocator();
  std::unordered_map<EMaterialSetType,Ref<DescriptorSet>> sets;
  for(auto layout : layouts) {
    if(layout.first == EMaterialSetType::Dynamic) {
      continue;
    }

    sets.insert({layout.first,globalAllocator->Allocate(layout.second)});
  }
  
  instance->SetSets(sets,layouts);
  instance->SetResources(resources);
  instance->Init(drawer);
  
  return instance;
}
}
