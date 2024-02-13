#pragma once
#include "LightComponent.hpp"
#include "generated/scene/components/PointLightComponent.reflect.hpp"

namespace vengine::scene {
RCLASS()

class PointLightComponent : public LightComponent {

  drawing::GpuLight GetLightInfo() override;
public:
  RFUNCTION()
  static Managed<PointLightComponent> Construct() {
    return newManagedObject<PointLightComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(PointLightComponent)
};

REFLECT_IMPLEMENT(PointLightComponent)
}
