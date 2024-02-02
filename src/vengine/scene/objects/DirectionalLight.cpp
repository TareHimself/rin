#include <vengine/scene/objects/DirectionalLight.hpp>
#include "vengine/scene/components/DirectionalLightComponent.hpp"

namespace vengine::scene {

Pointer<SceneComponent> DirectionalLight::CreateRootComponent() {
  return newSharedObject<DirectionalLightComponent>();
}

void DirectionalLight::AttachComponentsToRoot(const WeakPointer<SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard.Reserve()->AttachTo(root);
}
}
