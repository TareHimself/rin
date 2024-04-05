#pragma once
#include "Light.hpp"
#include "aerox/scene/components/BillboardComponent.hpp"
#include "gen/scene/objects/DirectionalLight.gen.hpp"
namespace aerox::scene {

META_TYPE()
class DirectionalLight : public Light {
public:

  META_BODY()
  
  std::shared_ptr<SceneComponent> CreateRootComponent() override;

  META_FUNCTION()
  static std::shared_ptr<DirectionalLight> Construct() {
    return newObject<DirectionalLight>();
  }
};
}
