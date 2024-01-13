#pragma once
#include "vengine/drawing/types.hpp"

namespace vengine {
namespace drawing {
struct SceneFrameData {
private:
  FrameData * _frame = nullptr;
  vk::DescriptorSet _sceneDescriptor;
public:
  SceneFrameData(FrameData * frame);

  vk::CommandBuffer * getCmd() const;

  vk::DescriptorSet getSceneDescriptor() const;
  
  void setSceneDescriptor(const vk::DescriptorSet &descriptor);

  CleanupQueue * getCleaner();
};
}
}
