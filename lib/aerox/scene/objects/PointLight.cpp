#include <aerox/scene/objects/PointLight.hpp>
#include "aerox/scene/components/PointLightComponent.hpp"
namespace aerox::scene {

std::shared_ptr<SceneComponent> PointLight::CreateRootComponent() {
  return newObject<PointLightComponent>();
}

}
