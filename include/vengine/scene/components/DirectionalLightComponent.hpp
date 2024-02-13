#pragma once
#include "LightComponent.hpp"
#include "generated/scene/components/DirectionalLightComponent.reflect.hpp"

namespace vengine::scene {
RCLASS()
class DirectionalLightComponent : public LightComponent {

  drawing::GpuLight GetLightInfo() override;

public:
  RFUNCTION()
  static Managed<DirectionalLightComponent> Construct() {
    return newManagedObject<DirectionalLightComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(DirectionalLightComponent)
};
REFLECT_IMPLEMENT(DirectionalLightComponent)

}
