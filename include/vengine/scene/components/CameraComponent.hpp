#pragma once
#include "Component.hpp"
#include "RenderedComponent.hpp"
#include <glm/glm.hpp>
#include "generated/scene/components/CameraComponent.reflect.hpp"

namespace vengine::scene {
RCLASS()
class CameraComponent : public RenderedComponent {
public:
  float fieldOfView = 70.0f;
  float nearClipPlane = 0.1f;
  float farClipPlane = 10000.0f;

  glm::mat4 GetViewMatrix() const;

  glm::mat4 GetProjection(float aspectRatio) const;

  void Draw(
      drawing::SimpleFrameData *frameData,
      const math::Transform &parentTransform) override;

  void SetRelativeTransform(const math::Transform &val) override;
  
  RFUNCTION()
  static Managed<CameraComponent> Construct() {
    return newManagedObject<CameraComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(CameraComponent)
};
REFLECT_IMPLEMENT(CameraComponent)

}
