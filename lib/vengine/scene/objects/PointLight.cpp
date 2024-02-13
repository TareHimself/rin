#include <vengine/scene/objects/PointLight.hpp>
#include "vengine/scene/components/PointLightComponent.hpp"
namespace vengine::scene {

Managed<SceneComponent> PointLight::CreateRootComponent() {
  return newManagedObject<PointLightComponent>();
}

}
