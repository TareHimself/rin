#include "SceneComponent.hpp"

#include "vengine/scene/SceneObject.hpp"

namespace vengine::scene {

math::Transform SceneComponent::GetRelativeTransform() const {
  return _relativeTransform;
}

void SceneComponent::SetRelativeTransform(const math::Transform &val) {
  _relativeTransform = val;
}

Transformable * SceneComponent::GetParent() const {
  return _parent;
}

void SceneComponent::AttachTo(SceneComponent * parent) {
  _parent = parent;
}
}
