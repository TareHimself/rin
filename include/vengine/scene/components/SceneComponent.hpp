#pragma once
#include "Component.hpp"
#include "vengine/containers/Set.hpp"
#include "vengine/math/Transform.hpp"
#include "generated/scene/components/SceneComponent.reflect.hpp"
namespace vengine::scene {
RCLASS()
class SceneComponent : public Component {
  math::Transform _relativeTransform;
  SceneComponent *_parent = nullptr;
  Set<Ref<SceneComponent>> _children;

public:
  // Transformable Interface
  virtual math::Vector GetRelativeLocation() const;
  virtual math::Quat GetRelativeRotation() const;
  virtual math::Vector GetRelativeScale() const;
  virtual math::Transform GetRelativeTransform() const;
  virtual math::Vector GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vector GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;

  virtual void SetRelativeLocation(const math::Vector &val);
  virtual void SetRelativeRotation(const math::Quat &val);
  virtual void SetRelativeScale(const math::Vector &val);
  virtual void SetRelativeTransform(const math::Transform &val);
  virtual void SetRelativeTransform(const std::optional<math::Vector> &location,
                                    const std::optional<math::Quat> &rotation,
                                    const std::optional<math::Vector> &scale);
  virtual void SetWorldLocation(const math::Vector &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vector &val);
  virtual void SetWorldTransform(const math::Transform &val);
  virtual void SetWorldTransform(const std::optional<math::Vector> &location,
                                 const std::optional<math::Quat> &rotation,
                                 const std::optional<math::Vector> &scale);

  virtual SceneComponent *GetParent() const;

  virtual void AttachTo(const Ref<SceneComponent> &parent);

  RFUNCTION()
  static Managed<SceneComponent> Construct() {
    return newManagedObject<SceneComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(SceneComponent)
};


REFLECT_IMPLEMENT(SceneComponent)

}
