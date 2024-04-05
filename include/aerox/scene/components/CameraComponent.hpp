#pragma once
#include "Component.hpp"
#include "RenderedComponent.hpp"
#include <glm/glm.hpp>
#include "gen/scene/components/CameraComponent.gen.hpp"

namespace aerox::scene {
META_TYPE()
class CameraComponent : public RenderedComponent {
public:

  META_BODY()
  
  float fieldOfView = 70.0f;
  float nearClipPlane = 0.1f;
  float farClipPlane = 10000.0f;

  glm::mat4 GetViewMatrix() const;

  glm::mat4 GetProjection(float aspectRatio) const;

  void Draw(
      drawing::SceneFrameData *frameData,
      const math::Transform &parentTransform) override;

  void SetRelativeTransform(const math::Transform &val) override;
  
  META_FUNCTION()
  static std::shared_ptr<CameraComponent> Construct() {
    return newObject<CameraComponent>();
  }
};

}
