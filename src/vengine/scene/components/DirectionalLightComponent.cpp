#include "vengine/drawing/scene/types.hpp"

#include <vengine/scene/components/DirectionalLightComponent.hpp>
namespace vengine::scene {


drawing::GpuLight DirectionalLightComponent::GetLightInfo() {
  drawing::GpuLight info{};
  
  info.color = glm::vec4{1.0,1.0,1.0,_intensity};
  const auto worldLocation = GetWorldLocation();
  info.location = glm::vec4{worldLocation.x,worldLocation.y,worldLocation.z,0.0f};
  const auto forwardDirection = GetWorldRotation().Forward();
  info.direction = glm::vec4{forwardDirection.x,forwardDirection.y,forwardDirection.z,1.0};
  return info;
}
}
