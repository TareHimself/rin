#pragma once
#include "Component.hpp"
#include "vengine/math/Transform.hpp"
#include "vengine/math/Transform.hpp"

namespace vengine {
namespace scene {
class SceneComponent : public Component {
  math::Transform _relativeTransform;
  SceneComponent * _parent = nullptr;
public:
  
  math::Transform getRelativeTransform() const;
  void setRelativeTransform(const math::Transform &transform);

  math::Vector3 getRelativeLocation() const;
  math::Quaternion getRelativeRotation() const;
  math::Vector3 getRelativeScale() const;
  
  void setRelativeLocation(const math::Vector3 &location);
  void setRelativeRotation(const math::Quaternion &rotation);
  void setRelativeScale(const math::Vector3 &scale);

  void attachTo(SceneComponent * parent);
  
  math::Transform getWorldTransform() const;

  SceneComponent * getParent() const;
};
}
}
