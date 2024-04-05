#include <aerox/scene/objects/DirectionalLight.hpp>
#include "aerox/scene/components/DirectionalLightComponent.hpp"

namespace aerox::scene {

std::shared_ptr<SceneComponent> DirectionalLight::CreateRootComponent() {
  return newObject<DirectionalLightComponent>();
}

}
