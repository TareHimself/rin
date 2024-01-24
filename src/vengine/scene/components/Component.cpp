#include "Component.hpp"
#include <vengine/scene/SceneObject.hpp>

namespace vengine::scene {

SceneObject * Component::GetOwner() const {
  return GetOuter();
}

void Component::ReadFrom(Buffer &store) {
}

void Component::WriteTo(Buffer &store) {
}
}
