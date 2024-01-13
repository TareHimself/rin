#pragma once
#include "Component.hpp"
#include "RenderedComponent.hpp"

#include <glm/glm.hpp>

namespace vengine {
namespace scene {
class CameraComponent : public RenderedComponent {
public:
  
  glm::mat4 getProjection();
  
};
}
}
