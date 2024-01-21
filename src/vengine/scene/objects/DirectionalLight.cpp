#include "DirectionalLight.hpp"
#include "vengine/scene/components/DirectionalLightComponent.hpp"

namespace vengine::scene {

SceneComponent * DirectionalLight::CreateRootComponent() {
  return newObject<DirectionalLightComponent>();
}

void DirectionalLight::AttachComponentsToRoot(SceneComponent *root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard->AttachTo(root);
}
}
