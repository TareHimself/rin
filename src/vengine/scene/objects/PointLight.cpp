#include "PointLight.hpp"

#include "vengine/scene/components/PointLightComponent.hpp"

namespace vengine::scene {

SceneComponent * PointLight::CreateRootComponent() {
  return newObject<PointLightComponent>();
}

void PointLight::AttachComponentsToRoot(SceneComponent *root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard->AttachTo(root);
}
}
