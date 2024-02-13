#include <vengine/scene/objects/DirectionalLight.hpp>
#include "vengine/scene/components/DirectionalLightComponent.hpp"

namespace vengine::scene {

Managed<SceneComponent> DirectionalLight::CreateRootComponent() {
  return newManagedObject<DirectionalLightComponent>();
}

}
