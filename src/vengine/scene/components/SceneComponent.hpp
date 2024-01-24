#pragma once
#include "Component.hpp"
#include "Transformable.hpp"
#include "vengine/math/Transform.hpp"
#include "vengine/math/Transform.hpp"

namespace vengine::scene {
class SceneComponent : public Component, public Transformable {
  math::Transform _relativeTransform;
  SceneComponent * _parent = nullptr;
public:
  
  // Transformable Interface
  math::Transform GetRelativeTransform() const override;
  void SetRelativeTransform(const math::Transform &val) override;
  Transformable * GetParent() const override;

  
  virtual void AttachTo(SceneComponent* parent);

  VENGINE_IMPLEMENT_COMPONENT_ID(SceneComponent)
};


}
