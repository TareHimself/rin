#include "types.hpp"

namespace vengine {
namespace drawing {
SceneFrameData::SceneFrameData(FrameData *frame) {
  _frame  = frame;
}

vk::CommandBuffer * SceneFrameData::getCmd() const {
  return _frame->getCmd();
}

vk::DescriptorSet SceneFrameData::getSceneDescriptor() const{
  return _sceneDescriptor;
}

void SceneFrameData::setSceneDescriptor(const vk::DescriptorSet &descriptor) {
  _sceneDescriptor = descriptor;
}

CleanupQueue * SceneFrameData::getCleaner() {
  return &_frame->cleaner;
}
}
}