#pragma once
#include "vengine/scene/SceneObject.hpp"
#include "vengine/scene/components/CameraComponent.hpp"

namespace vengine::scene {
class DefaultCamera : public SceneObject {
public:
  CameraComponent * camera = nullptr;

  SceneComponent * CreateRootComponent() override;

  void Init(scene::Scene *outer) override;

  float inputForward = 0.0f;
  float inputRight = 0.0f;
  float pitch = 0.0f;
  float yaw = 0.0f;
  bool bWantsToGoUp;

  void Update(float deltaTime) override;

  void UpdateRotation();

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(DefaultCamera)
};


}
