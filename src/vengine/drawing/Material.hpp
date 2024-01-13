#pragma once
#include "Drawer.hpp"
#include "vengine/Object.hpp"

namespace vengine {
namespace drawing {
struct SceneFrameData;
}
}

namespace vengine {
namespace drawing {
class Material : public Object<Drawer> {
  vk::Pipeline _pipeline;
  vk::PipelineLayout _layout;
  vk::DescriptorSet _materialSet;
  vk::DescriptorSetLayout _materialSetLayout;
  EMaterialPass _passType;

public:

  struct  MaterialConstants {
    glm::vec4 colorFactors;
    glm::vec4 metalRoughFactors;

    // padding
    glm::vec4 extra[14];
  };

  struct  MaterialResources {
    AllocatedImage color;
    vk::Sampler colorSampler;
    AllocatedImage metallic;
    vk::Sampler metallicSampler;
    vk::Buffer dataBuffer;
    uint32_t dataBufferOffset;
  };

  void init(Drawer *drawer) override;

  virtual void init(Drawer *drawer, EMaterialPass pass,
    DescriptorAllocatorGrowable allocator, const MaterialResources &resources);

  static Material * create(Drawer * drawer, EMaterialPass pass, const DescriptorAllocatorGrowable &allocator,const MaterialResources& resources);

  void bind(const SceneFrameData * frame) const;

  void pushVertexConstant(const SceneFrameData * frame,size_t size, const void * data) const;
};
}
}
