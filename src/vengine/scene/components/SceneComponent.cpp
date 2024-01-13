#include "SceneComponent.hpp"

#include "vengine/scene/SceneObject.hpp"

namespace vengine {
namespace scene {
math::Transform SceneComponent::getRelativeTransform() const {
  return _relativeTransform;
}

void SceneComponent::setRelativeTransform(const math::Transform &transform) {
  _relativeTransform = transform;
}

math::Vector3 SceneComponent::getRelativeLocation() const {
  return getRelativeTransform().location;
}

math::Quaternion SceneComponent::getRelativeRotation() const {
  return getRelativeTransform().rotation;
}

math::Vector3 SceneComponent::getRelativeScale() const {
  return getRelativeTransform().scale;
}

void SceneComponent::setRelativeLocation(const math::Vector3 &location) {
  _relativeTransform.location = location;
}

void SceneComponent::setRelativeRotation(const math::Quaternion &rotation) {
  _relativeTransform.rotation = rotation;
}

void SceneComponent::setRelativeScale(const math::Vector3 &scale) {
  _relativeTransform.scale = scale;
}

void SceneComponent::attachTo(SceneComponent *parent) {
  _parent = parent;
}

math::Transform SceneComponent::getWorldTransform() const {
  if(const auto parent = getParent()) {
    return getRelativeTransform().relativeTo(parent->getRelativeTransform());
  }
  return _relativeTransform;
}

SceneComponent * SceneComponent::getParent() const {
  return _parent;
}
}
}
