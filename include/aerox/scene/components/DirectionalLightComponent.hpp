#pragma once
#include "LightComponent.hpp"
#include "gen/scene/components/DirectionalLightComponent.gen.hpp"

namespace aerox::scene {
META_TYPE()
class DirectionalLightComponent : public LightComponent {

  drawing::GpuLight GetLightInfo() override;

public:

  META_BODY()
  
  META_FUNCTION()
  static std::shared_ptr<DirectionalLightComponent> Construct() {
    return newObject<DirectionalLightComponent>();
  }
};

}
