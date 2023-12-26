#include "Component.hpp"
#include <vengine/scene/SceneObject.hpp>

namespace vengine {
namespace scene {

void Component::init(SceneObject * owner) {
  Object::init(owner);
}
}
}
