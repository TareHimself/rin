#include <vengine/scene/objects/PointLight.hpp>
#include "vengine/scene/components/PointLightComponent.hpp"
namespace vengine::scene {

Pointer<SceneComponent> PointLight::CreateRootComponent() {
  return newSharedObject<PointLightComponent>();
}

void PointLight::AttachComponentsToRoot(const WeakPointer<SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard.Reserve()->AttachTo(root);
}
}
