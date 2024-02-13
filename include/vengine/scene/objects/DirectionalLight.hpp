#pragma once
#include "Light.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"
#include "generated/scene/objects/DirectionalLight.reflect.hpp"
namespace vengine::scene {

RCLASS()
class DirectionalLight : public Light {
public:
  Managed<SceneComponent> CreateRootComponent() override;

  RFUNCTION()
  static Managed<DirectionalLight> Construct() {
    return newManagedObject<DirectionalLight>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(DirectionalLight)
};

REFLECT_IMPLEMENT(DirectionalLight)
}
