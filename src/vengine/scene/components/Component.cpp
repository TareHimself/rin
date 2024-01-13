#include "Component.hpp"
#include <vengine/scene/SceneObject.hpp>

namespace vengine {
namespace scene {

SceneObject * Component::getOwner() const {
  return getOuter();
}
}
}
