#pragma once
#include <vengine/scene/objects/SceneObject.hpp>
#include "vengine/scene/components/CameraComponent.hpp"
#include "generated/scene/objects/DefaultCamera.reflect.hpp"
namespace vengine::scene {
RCLASS()
class DefaultCamera : public SceneObject {
public:
  Ref<CameraComponent> camera;

  Managed<SceneComponent> CreateRootComponent() override;

  void Init(scene::Scene *outer) override;

  float inputForward = 0.0f;
  float inputRight = 0.0f;
  float pitch = 0.0f;
  float yaw = 0.0f;
  bool bWantsToGoUp;

  void Tick(float deltaTime) override;

  void UpdateRotation();

  RFUNCTION()
  static Managed<CameraComponent> Construct() {
    return newManagedObject<CameraComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(CameraComponent)
};

REFLECT_IMPLEMENT(DefaultCamera)
}
