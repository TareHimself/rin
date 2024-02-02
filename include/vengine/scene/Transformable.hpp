#pragma once
#include "vengine/Pointer.hpp"
#include "vengine/math/Transform.hpp"

namespace vengine::scene {

class Transformable {
  math::Transform _cachedWorldTransform{};
  math::Transform _cachedRelativeTransform{};
public:
  virtual math::Vector GetRelativeLocation() const;
  virtual math::Quat GetRelativeRotation() const;
  virtual math::Vector GetRelativeScale() const;
  virtual math::Transform GetRelativeTransform() const = 0;
  virtual math::Vector GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vector GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;

  // Only use for cross threading i.e. drawing as it may be outdated
  math::Transform GetCachedWorldTransform() const;

  // Only use for cross threading i.e. drawing as it may be outdated
  math::Transform GetCachedRelativeTransform() const;
  
  virtual void SetRelativeLocation(const math::Vector &val);
  virtual void SetRelativeRotation(const math::Quat &val);
  virtual void SetRelativeScale(const math::Vector &val);
  virtual void SetRelativeTransform(const math::Transform &val);
  virtual void SetWorldLocation(const math::Vector &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vector &val);
  virtual void SetWorldTransform(const math::Transform &val);
  
  virtual WeakPointer<Transformable> GetParent() const = 0;
};
}
