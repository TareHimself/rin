#include <vengine/scene/components/LightComponent.hpp>
namespace vengine::scene {
void LightComponent::SetIntensity(float intensity) {
  _intensity = intensity;
}

void LightComponent::SetRadius(float radius) {
  _radius = radius;
}

void LightComponent::SetColor(glm::vec4 color) {
  _color = color;
}

float LightComponent::GetIntensity() const {
  return _intensity;
}

glm::vec4 LightComponent::GetColor() const {
  return _color;
}
}
