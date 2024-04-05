#pragma once
#include "SceneComponent.hpp"
#include "gen/scene/components/LightComponent.gen.hpp"

namespace aerox::drawing {
struct GpuLight;
}

namespace aerox::scene {
META_TYPE()
class LightComponent : public SceneComponent {
protected:
  float _intensity = 1.0f;
  float _radius = 1.0f;
  glm::vec4 _color{1.0f};
public:

  META_BODY()
  
  void SetIntensity(float intensity);
  void SetRadius(float radius);
  void SetColor(glm::vec4 color);

  float GetIntensity() const;
  glm::vec4 GetColor() const;

  virtual drawing::GpuLight GetLightInfo() = 0;
};
}
