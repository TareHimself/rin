#pragma once
#include "Light.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"
#include "generated/scene/objects/PointLight.reflect.hpp"
namespace vengine::scene {

RCLASS()
class PointLight : public Light {
  
public:
  
  Managed<SceneComponent> CreateRootComponent() override;
  
  RFUNCTION()
  static Managed<PointLight> Construct() {
    return newManagedObject<PointLight>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(PointLight)
};
REFLECT_IMPLEMENT(PointLight)
}
