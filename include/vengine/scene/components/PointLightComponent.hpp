#pragma once
#include "LightComponent.hpp"

namespace vengine::scene {
class PointLightComponent : public LightComponent {
  VENGINE_IMPLEMENT_COMPONENT_ID(PointLightComponent)

  drawing::GpuLight GetLightInfo() override;
};


}
