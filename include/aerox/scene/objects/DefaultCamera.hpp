#pragma once
#include <aerox/scene/objects/SceneObject.hpp>
#include "aerox/scene/components/CameraComponent.hpp"
#include "gen/scene/objects/DefaultCamera.gen.hpp"
namespace aerox::scene {
META_TYPE()
class DefaultCamera : public SceneObject {
public:

  META_BODY()
  
  std::weak_ptr<CameraComponent> camera;

  std::shared_ptr<SceneComponent> CreateRootComponent() override;

  void OnInit(Scene * scene) override;

  float inputForward = 0.0f;
  float inputRight = 0.0f;
  float pitch = 0.0f;
  float yaw = 0.0f;
  bool bWantsToGoUp;

  void Tick(float deltaTime) override;

  void UpdateRotation();

  META_FUNCTION()
  static std::shared_ptr<CameraComponent> Construct() {
    return newObject<CameraComponent>();
  }
};

}
