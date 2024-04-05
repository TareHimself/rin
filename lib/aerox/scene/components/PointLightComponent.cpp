#include "aerox/drawing/scene/types.hpp"

#include <aerox/scene/components/PointLightComponent.hpp>
namespace aerox::scene {

drawing::GpuLight PointLightComponent::GetLightInfo() {
  drawing::GpuLight info{};
  info.color = glm::vec4{1.0,1.0,1.0,_intensity};
  const auto worldLocation = GetWorldLocation();
  info.location = glm::vec4{worldLocation.x,worldLocation.y,worldLocation.z,0.0f};
  info.direction = glm::vec4{0.0f,-1.0f,0.0f,0.0};
  return info;
}
}
