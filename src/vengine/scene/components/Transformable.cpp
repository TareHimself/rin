#include "Transformable.hpp"

namespace vengine::scene {
math::Vector Transformable::GetRelativeLocation() const {
  return GetRelativeTransform().location;
}

math::Quat Transformable::GetRelativeRotation() const {
  return GetRelativeTransform().rotation;
}

math::Vector Transformable::GetRelativeScale() const {
  return GetRelativeTransform().scale;
}

void Transformable::SetRelativeLocation(const math::Vector &val) {
  SetRelativeTransform({val,GetRelativeRotation(),GetRelativeScale()});
}

void Transformable::SetRelativeRotation(const math::Quat &val) {
  SetRelativeTransform({GetRelativeLocation(),val,GetRelativeScale()});
}

void Transformable::SetRelativeScale(const math::Vector &val) {
  SetRelativeTransform({GetRelativeLocation(),GetRelativeRotation(),val});
}

math::Vector Transformable::GetWorldLocation() const {
  return GetWorldTransform().location;
}

math::Quat Transformable::GetWorldRotation() const {
  return GetWorldTransform().rotation;
}

math::Vector Transformable::GetWorldScale() const {
  return GetWorldTransform().scale;
}

math::Transform Transformable::GetWorldTransform() const {
  if(const auto parent = GetParent()) {
    return GetRelativeTransform().RelativeTo(parent->GetWorldTransform());
  }

  return GetRelativeTransform();
}

void Transformable::SetWorldLocation(const math::Vector &val) {
  SetWorldTransform({val,GetWorldRotation(),GetWorldScale()});
}

void Transformable::SetWorldRotation(const math::Quat &val) {
  SetWorldTransform({GetWorldLocation(),val,GetWorldScale()});
}

void Transformable::SetWorldScale(const math::Vector &val) {
  SetWorldTransform({GetWorldLocation(),GetWorldRotation(),val});
}

void Transformable::SetWorldTransform(const math::Transform &val) {
  if(GetParent()) {
    const auto targetWorldMatrix = val.Matrix();
    const auto thisWorldMatrix = GetWorldTransform().Matrix();

    const auto thisToTarget = inverse(thisWorldMatrix) * targetWorldMatrix;
    const auto thisToParent = GetRelativeTransform().Matrix();
    
    SetRelativeTransform(thisToTarget * inverse(thisToParent));
  }

  SetRelativeTransform(val);
}
}
