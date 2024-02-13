#pragma once
#include "SceneComponent.hpp"

namespace vengine {
namespace drawing {
struct GpuLight;
}
}

namespace vengine::scene {
class LightComponent : public SceneComponent {
protected:
  float _intensity = 1.0f;
  float _radius = 1.0f;
  glm::vec4 _color{1.0f};
public:
  void SetIntensity(float intensity);
  void SetRadius(float radius);
  void SetColor(glm::vec4 color);

  float GetIntensity() const;
  glm::vec4 GetColor() const;

  virtual drawing::GpuLight GetLightInfo() = 0;
};
}
