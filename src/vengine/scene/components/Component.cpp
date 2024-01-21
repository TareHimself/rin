#include "Component.hpp"
#include <vengine/scene/SceneObject.hpp>

namespace vengine::scene {

SceneObject * Component::GetOwner() const {
  return GetOuter();
}
}
