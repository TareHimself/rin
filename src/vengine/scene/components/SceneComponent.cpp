#include <vengine/scene/components/SceneComponent.hpp>
#include "vengine/scene/objects/SceneObject.hpp"

namespace vengine::scene {

math::Transform SceneComponent::GetRelativeTransform() const {
  return _relativeTransform;
}

void SceneComponent::SetRelativeTransform(const math::Transform &val) {
  Transformable::SetRelativeTransform(val);
  _relativeTransform = val;
}

WeakPointer<Transformable> SceneComponent::GetParent() const {
  return _parent;
}

void SceneComponent::AttachTo(const WeakPointer<SceneComponent> &parent) {
  _parent = parent;
}
}
