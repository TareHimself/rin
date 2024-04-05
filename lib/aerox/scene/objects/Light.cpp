#include "aerox/scene/components/LightComponent.hpp"

#include <aerox/scene/objects/Light.hpp>

namespace aerox::scene {
void Light::SetIntensity(const float val) {
  if(const auto lightComp = utils::cast<LightComponent>(GetRootComponent().lock())) {
    lightComp->SetIntensity(val);
  }
}

float Light::GetIntensity() const {
  if(const auto lightComp = utils::cast<LightComponent>(GetRootComponent().lock())) {
    return lightComp->GetIntensity();
  }

  return -1.0f;
}

void Light::SetColor(const glm::vec4 &color) {
  if(const auto lightComp = utils::cast<LightComponent>(GetRootComponent().lock())) {
    lightComp->SetColor(color);
  }
}

glm::vec4 Light::GetColor() const {
  if(const auto lightComp = utils::cast<LightComponent>(GetRootComponent().lock())) {
    return lightComp->GetColor();
  }

  return glm::vec4{1.0f};
}

void Light::AttachComponentsToRoot(const std::weak_ptr<SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  if(const auto lRoot = root.lock()) {
    if(const auto billboard = _billboard.lock()) {
      billboard->AttachTo(lRoot);
    }
  }
}
}
