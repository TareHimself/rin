#include <vengine/scene/objects/DirectionalLight.hpp>
#include "vengine/scene/components/DirectionalLightComponent.hpp"

namespace vengine::scene {

Ref<SceneComponent> DirectionalLight::CreateRootComponent() {
  return newSharedObject<DirectionalLightComponent>();
}

}
