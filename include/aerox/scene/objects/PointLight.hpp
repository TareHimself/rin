#pragma once
#include "Light.hpp"
#include "aerox/scene/objects/SceneObject.hpp"
#include "aerox/scene/components/BillboardComponent.hpp"
#include "gen/scene/objects/PointLight.gen.hpp"

namespace aerox::scene {

META_TYPE()
class PointLight : public Light {
  
public:
  META_BODY()
  
  std::shared_ptr<SceneComponent> CreateRootComponent() override;
  
  META_FUNCTION()
  static std::shared_ptr<PointLight> Construct() {
    return newObject<PointLight>();
  }

  
};
}
