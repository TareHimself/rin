#include <aerox/scene/components/SceneComponent.hpp>
#include "aerox/scene/objects/SceneObject.hpp"

namespace aerox::scene {

math::Vec3<> SceneComponent::GetRelativeLocation() const {
  return GetWorldTransform().location;
}

math::Quat SceneComponent::GetRelativeRotation() const {
  return GetWorldTransform().rotation;
}

math::Vec3<> SceneComponent::GetRelativeScale() const {
  return GetRelativeTransform().scale;
}

math::Vec3<> SceneComponent::GetWorldLocation() const {
  return GetWorldTransform().location;
}

math::Quat SceneComponent::GetWorldRotation() const {
  return GetWorldTransform().rotation;
}

math::Vec3<> SceneComponent::GetWorldScale() const {
  return GetWorldTransform().scale;
}

math::Transform SceneComponent::GetWorldTransform() const {
  if(const auto parent = GetParent()) {
    return GetRelativeTransform().RelativeTo(parent->GetWorldTransform());
  }

  return GetRelativeTransform();
  //return GetRelativeTransform();
}

void SceneComponent::SetRelativeLocation(const math::Vec3<> &val) {
  SetRelativeTransform(val,{},{});
}

void SceneComponent::SetRelativeRotation(const math::Quat &val) {
  SetRelativeTransform({},val,{});
}

void SceneComponent::SetRelativeScale(const math::Vec3<> &val) {
  SetRelativeTransform({},{},val);
}

void SceneComponent::SetWorldLocation(const math::Vec3<> &val) {
  SetWorldTransform(val,{},{});
}

void SceneComponent::SetWorldRotation(const math::Quat &val) {
  SetWorldTransform({},val,{});
}

void SceneComponent::SetWorldScale(const math::Vec3<> &val) {
  
  SetWorldTransform({},{},val);
}

void SceneComponent::SetWorldTransform(const math::Transform &val) {
  // _relativeTransform = val;
  if(GetParent()) {
    const auto targetWorldMatrix = val.Matrix();
    const auto thisWorldMatrix = GetWorldTransform().Matrix();

    const auto thisToTarget = inverse(thisWorldMatrix) * targetWorldMatrix;
    const auto thisToParent = GetRelativeTransform().Matrix();
    
    SetRelativeTransform(thisToTarget * inverse(thisToParent));
    return;
  }

  SetRelativeTransform(val);
}

void SceneComponent::SetWorldTransform(
    const std::optional<math::Vec3<>> &location,
    const std::optional<math::Quat> &rotation,
    const std::optional<math::Vec3<>> &scale) {
  auto existingTransform = GetWorldTransform();
  SetWorldTransform({location.value_or(existingTransform.location),rotation.value_or(existingTransform.rotation),scale.value_or(existingTransform.scale)});
}

math::Transform SceneComponent::GetRelativeTransform() const {
  return _relativeTransform;
}

void SceneComponent::SetRelativeTransform(const math::Transform &val) {
  _relativeTransform = val;
}

void SceneComponent::SetRelativeTransform(
    const std::optional<math::Vec3<>> &location,
    const std::optional<math::Quat> &rotation,
    const std::optional<math::Vec3<>> &scale) {
  auto existingTransform = GetWorldTransform();
  SetRelativeTransform({location.value_or(existingTransform.location),rotation.value_or(existingTransform.rotation),scale.value_or(existingTransform.scale)});
  
}

SceneComponent * SceneComponent::GetParent() const {
  return _parent;
}

void SceneComponent::AttachTo(const std::weak_ptr<SceneComponent> &parent) {
  _parent = parent.lock().get();
}
}
