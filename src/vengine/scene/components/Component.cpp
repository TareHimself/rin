#include "Component.hpp"

namespace vengine {
namespace scene {
void Component::init() {
  Object::init();
  
}

void Component::init(SceneObject *owningObject) {
  owner = owningObject;
  init();
}

SceneObject * Component::getOwner() {
  return owner;
}
}
}
