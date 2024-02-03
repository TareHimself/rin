#pragma once
#include "LightComponent.hpp"


namespace vengine::scene {
class DirectionalLightComponent : public LightComponent {
  VENGINE_IMPLEMENT_COMPONENT_ID(DirectionalLightComponent)

  drawing::GpuLight GetLightInfo() override;
};


}
