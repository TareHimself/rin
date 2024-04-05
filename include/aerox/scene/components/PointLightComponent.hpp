#pragma once
#include "LightComponent.hpp"
#include "gen/scene/components/PointLightComponent.gen.hpp"

namespace aerox::scene {
META_TYPE()

class PointLightComponent : public LightComponent {

  drawing::GpuLight GetLightInfo() override;
public:

  META_BODY()
  
  META_FUNCTION()
  static std::shared_ptr<PointLightComponent> Construct() {
    return newObject<PointLightComponent>();
  }
};
}
