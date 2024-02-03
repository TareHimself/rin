#pragma once
#include "Component.hpp"
#include "RenderedComponent.hpp"

#include <glm/glm.hpp>

namespace vengine::scene {
class CameraComponent : public RenderedComponent {
public:
  float fieldOfView = 70.0f;
  float nearClipPlane = 0.1f;
  float farClipPlane = 10000.0f;
  
  glm::mat4 GetViewMatrix() const;

  glm::mat4 GetProjection(float aspectRatio) const;
  
  void Draw(drawing::SceneDrawer *drawer, const math::Transform &parentTransform, drawing::SimpleFrameData *frameData) override;

  void SetRelativeTransform(const math::Transform &val) override;
  VENGINE_IMPLEMENT_COMPONENT_ID(CameraComponent)
};


}
