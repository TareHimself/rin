#pragma once
#include "Component.hpp"
#include <vengine/scene/Transformable.hpp>
#include "vengine/math/Transform.hpp"
#include "vengine/math/Transform.hpp"

namespace vengine::scene {
class SceneComponent : public Component, public Transformable {
  math::Transform _relativeTransform;
  WeakPointer<Transformable> _parent;
public:
  
  // Transformable Interface
  math::Transform GetRelativeTransform() const override;
  void SetRelativeTransform(const math::Transform &val) override;
  WeakPointer<Transformable> GetParent() const override;

  
  virtual void AttachTo(const WeakPointer<SceneComponent> &parent);

  VENGINE_IMPLEMENT_COMPONENT_ID(SceneComponent)
};


}
