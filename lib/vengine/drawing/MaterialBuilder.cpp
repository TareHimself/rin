#include "vengine/Engine.hpp"
#include "vengine/drawing/WindowDrawer.hpp"

#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/MaterialInstance.hpp>

namespace vengine::drawing {
MaterialBuilder &MaterialBuilder::AddShader(Managed<Shader> shader) {
  _pipelineBuilder.AddShaderStage(shader);
  _shaders.push(shader);
  return *this;
}

MaterialBuilder & MaterialBuilder::AddShaders(const Array<Managed<Shader>> &shaders) {
  for(const auto &shader : shaders) {
    AddShader(shader);
  }
  return *this;
}

MaterialBuilder & MaterialBuilder::AddAttachmentFormats(
    const Array<vk::Format> &formats) {
  for (auto &format : formats) {
    _pipelineBuilder.AddColorAttachment(format);
  } 

  return *this;
}

MaterialBuilder &MaterialBuilder::SetType(const EMaterialType type) {
  _type = type;
  return *this;
}

Array<vk::PushConstantRange> MaterialBuilder::ComputePushConstantRanges(
    ShaderResources &resources) {
  Array<vk::PushConstantRange> ranges;
  for (auto &info : resources.pushConstants | std::views::values) {
    ranges.push({info.stages, info.offset, info.size});
  }

  return ranges;
}

Managed<MaterialInstance> MaterialBuilder::Create() {
  auto drawer = Engine::Get()->GetDrawingSubsystem().Reserve();
  auto instance = newManagedObject<MaterialInstance>();
  ShaderResources resources{};

  std::unordered_map<EMaterialSetType, DescriptorLayoutBuilder> layoutBuilders;
  uint32_t maxLayout = 0;
  for (auto &shader : _shaders) {
    auto shaderResources = shader->GetResources();
    auto shaderStage = shader->GetStage();
    for (auto &val : shaderResources.images) {
      resources.images.insert(val);

      if (!layoutBuilders.contains(val.second.set)) {
        layoutBuilders.insert({val.second.set, {}});

        if (maxLayout < val.second.set) {
          maxLayout = val.second.set;
        }
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,
                                                vk::DescriptorType::eCombinedImageSampler,
                                                shaderStage, val.second.count);
    }

    for (auto &val : shaderResources.uniformBuffers) {
      resources.uniformBuffers.insert(val);

      if (!layoutBuilders.contains(val.second.set)) {
        layoutBuilders.insert({val.second.set, {}});

        if (maxLayout < val.second.set) {
          maxLayout = val.second.set;
        }
      }

      layoutBuilders[val.second.set].AddBinding(val.second.binding,
                                                vk::DescriptorType::eUniformBuffer,
                                                shaderStage, val.second.count);
    }

    for (const auto &key : shaderResources.pushConstants | std::views::keys) {
      if (!resources.pushConstants.contains(key)) {
        auto pConst = shaderResources.pushConstants[key];

        resources.pushConstants.emplace(
            key, PushConstantInfo{pConst.offset, pConst.size, shaderStage});
      } else {
        resources.pushConstants.find(key)->second.stages |= shaderStage;
      }
    }
  }

  auto pushConstants = ComputePushConstantRanges(resources);

  std::unordered_map<EMaterialSetType, vk::DescriptorSetLayout> layouts;
  Array<vk::DescriptorSetLayout> layoutsArr;
  for (auto i = 0; i < maxLayout + 1; i++) {
    auto layoutType = static_cast<EMaterialSetType>(i);
    auto layout = layoutBuilders.contains(layoutType)
                    ? layoutBuilders[layoutType].Build()
                    : DescriptorLayoutBuilder().Build();
    layouts.emplace(layoutType, layout);
    layoutsArr.push(layout);
  }
  //instance->SetSetLayout(_layoutBuilder.Build(drawer->GetDevice()));

  const auto pipelineLayoutInfo = vk::PipelineLayoutCreateInfo(
      {}, layoutsArr, pushConstants);

  instance->SetLayout(
      drawer->GetVirtualDevice().createPipelineLayout(pipelineLayoutInfo));
  
  if (_type == EMaterialType::UI) {
    _pipelineBuilder
        .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
        .SetPolygonMode(vk::PolygonMode::eFill)
        //.SetCullMode(vk::CullModeFlagBits::eNone, vk::FrontFace::eClockwise)
        .SetMultisamplingModeNone()
        .DisableDepthTest()
        .EnableBlendingAlphaBlend()
        .SetLayout(instance->GetLayout());
  } else {
    _pipelineBuilder
        .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
        .SetPolygonMode(vk::PolygonMode::eFill)
        .SetCullMode(vk::CullModeFlagBits::eNone, vk::FrontFace::eClockwise)
        .SetMultisamplingModeNone()
        .DisableBlending()
        .EnableDepthTest(true, vk::CompareOp::eLessOrEqual)
        .SetDepthFormat(vk::Format::eD32Sfloat)
        .SetLayout(instance->GetLayout());

    if (_type == EMaterialType::Translucent) {
      _pipelineBuilder
          .EnableBlendingAdditive()
          .EnableDepthTest(false, vk::CompareOp::eLessOrEqual);
    }
  }

  instance->SetType(_type);

  instance->SetPipeline(_pipelineBuilder.Build(drawer->GetVirtualDevice()));

  auto globalAllocator = drawer->GetGlobalDescriptorAllocator();
  std::unordered_map<EMaterialSetType, Ref<DescriptorSet>> sets;
  for (auto layout : layouts) {
    if (layout.first == EMaterialSetType::Dynamic) {
      continue;
    }

    sets.insert({layout.first, globalAllocator->Allocate(layout.second)});
  }

  instance->SetSets(sets, layouts);
  instance->SetResources(resources);
  instance->Init(drawer.Get());

  return instance;
}
}
