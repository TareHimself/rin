#pragma once
#include "vengine/math/Transform.hpp"

namespace vengine::scene {

class Transformable {

public:
  virtual math::Vector GetRelativeLocation() const;
  virtual math::Quat GetRelativeRotation() const;
  virtual math::Vector GetRelativeScale() const;
  virtual math::Transform GetRelativeTransform() const = 0;
  virtual math::Vector GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vector GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;
  
  virtual void SetRelativeLocation(const math::Vector &val);
  virtual void SetRelativeRotation(const math::Quat &val);
  virtual void SetRelativeScale(const math::Vector &val);
  virtual void SetRelativeTransform(const math::Transform &val) = 0;
  virtual void SetWorldLocation(const math::Vector &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vector &val);
  virtual void SetWorldTransform(const math::Transform &val);
  
  virtual Transformable * GetParent() const = 0;
};
}
