#pragma once
#include "SceneComponent.hpp"

namespace vengine::scene {
class LightComponent : public SceneComponent {
public:
  float _intensity = 1.0f;
  float _radius = 1.0f;
  glm::vec4 _color{1.0f};
public:
  void SetIntensity(float intensity);
  void SetRadius(float radius);
  void SetColor(glm::vec4 color);
};
}
