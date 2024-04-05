#include <aerox/scene/components/Component.hpp>
#include <aerox/scene/objects/SceneObject.hpp>

namespace aerox::scene {

void Component::OnInit(SceneObject *owner) {
  TOwnedBy::OnInit(owner);
}

void Component::ReadFrom(Buffer &store) {
}

void Component::WriteTo(Buffer &store) {
}
}
