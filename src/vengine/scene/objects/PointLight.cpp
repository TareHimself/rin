#include <vengine/scene/objects/PointLight.hpp>
#include "vengine/scene/components/PointLightComponent.hpp"
namespace vengine::scene {

Ref<SceneComponent> PointLight::CreateRootComponent() {
  return newSharedObject<PointLightComponent>();
}

}
