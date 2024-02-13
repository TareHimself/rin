#include "vengine/scene/components/LightComponent.hpp"

#include <vengine/scene/objects/Light.hpp>

namespace vengine::scene {
void Light::SetIntensity(const float val) {
  if(auto lightComp = GetRootComponent().Reserve().Cast<LightComponent>()) {
    lightComp->SetIntensity(val);
  }
}

float Light::GetIntensity() const {
  if(auto lightComp = GetRootComponent().Reserve().Cast<LightComponent>()) {
    return lightComp->GetIntensity();
  }

  return -1.0f;
}

void Light::SetColor(const glm::vec4 &color) {
  if(auto lightComp = GetRootComponent().Reserve().Cast<LightComponent>()) {
    lightComp->SetColor(color);
  }
}

glm::vec4 Light::GetColor() const {
  if(auto lightComp = GetRootComponent().Reserve().Cast<LightComponent>()) {
    return lightComp->GetColor();
  }

  return glm::vec4{1.0f};
}

void Light::AttachComponentsToRoot(const Ref<SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard.Reserve()->AttachTo(root);
}
}
