#pragma once
#include "Component.hpp"
#include "aerox/containers/Set.hpp"
#include "aerox/math/Transform.hpp"
#include "gen/scene/components/SceneComponent.gen.hpp"
namespace aerox::scene {
META_TYPE()
class SceneComponent : public Component {
  math::Transform _relativeTransform;
  SceneComponent *_parent = nullptr;
  Set<std::weak_ptr<SceneComponent>> _children;

public:
  META_BODY()
  // Transformable Interface
  virtual math::Vec3<> GetRelativeLocation() const;
  virtual math::Quat GetRelativeRotation() const;
  virtual math::Vec3<> GetRelativeScale() const;
  virtual math::Transform GetRelativeTransform() const;
  virtual math::Vec3<> GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vec3<> GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;

  virtual void SetRelativeLocation(const math::Vec3<> &val);
  virtual void SetRelativeRotation(const math::Quat &val);
  virtual void SetRelativeScale(const math::Vec3<> &val);
  virtual void SetRelativeTransform(const math::Transform &val);
  virtual void SetRelativeTransform(const std::optional<math::Vec3<>> &location,
                                    const std::optional<math::Quat> &rotation,
                                    const std::optional<math::Vec3<>> &scale);
  virtual void SetWorldLocation(const math::Vec3<> &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vec3<> &val);
  virtual void SetWorldTransform(const math::Transform &val);
  virtual void SetWorldTransform(const std::optional<math::Vec3<>> &location,
                                 const std::optional<math::Quat> &rotation,
                                 const std::optional<math::Vec3<>> &scale);

  virtual SceneComponent *GetParent() const;

  virtual void AttachTo(const std::weak_ptr<SceneComponent> &parent);

  META_FUNCTION()
  static std::shared_ptr<SceneComponent> Construct() {
    return newObject<SceneComponent>();
  }
};
}
