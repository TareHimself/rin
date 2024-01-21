#include "types.hpp"

namespace vengine::drawing {
SceneFrameData::SceneFrameData(FrameData *frame) {
  _frame  = frame;
}

vk::CommandBuffer * SceneFrameData::GetCmd() const {
  return _frame->GetCmd();
}

vk::DescriptorSet SceneFrameData::GetSceneDescriptor() const{
  return _sceneDescriptor;
}

void SceneFrameData::SetSceneDescriptor(const vk::DescriptorSet &descriptor) {
  _sceneDescriptor = descriptor;
}

CleanupQueue * SceneFrameData::GetCleaner() const {
  return &_frame->cleaner;
}
}
