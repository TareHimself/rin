#include <vengine/scene/components/Component.hpp>
#include <vengine/scene/objects/SceneObject.hpp>

namespace vengine::scene {

SceneObject *Component::GetOwner() const {
  return GetOuter();
}

void Component::ReadFrom(Buffer &store) {
}

void Component::WriteTo(Buffer &store) {
}
}
