#ifndef VENGINE_DRAWING_SCENE_TYPES
#define VENGINE_DRAWING_SCENE_TYPES
#include "vengine/drawing/types.hpp"

namespace vengine::drawing {
struct SceneGpuData {
  glm::mat4 viewMatrix;
  glm::mat4 projectionMatrix;
  glm::vec4 ambientColor{1.0f,1.0f,1.0f,0.02f};
  glm::vec4 lightDirection{0.0f,-1.0f,0,0.0f};
  glm::vec4 cameraLocation{0.0f};
};

struct SceneFrameData {
private:
  FrameData * _frame = nullptr;
  vk::DescriptorSet _sceneDescriptor;
public:
  SceneFrameData(FrameData * frame);

  vk::CommandBuffer * GetCmd() const;

  vk::DescriptorSet GetSceneDescriptor() const;
  
  void SetSceneDescriptor(const vk::DescriptorSet &descriptor);

  CleanupQueue * GetCleaner() const;
};
}
#endif