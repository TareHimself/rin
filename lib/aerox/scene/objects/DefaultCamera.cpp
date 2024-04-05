#include <aerox/scene/objects/DefaultCamera.hpp>
#include "aerox/Engine.hpp"
#include "aerox/input/KeyInputEvent.hpp"
#include "aerox/math/constants.hpp"
#include "aerox/scene/Scene.hpp"

namespace aerox::scene {
std::shared_ptr<SceneComponent> DefaultCamera::CreateRootComponent() {
  auto result = newObject<CameraComponent>();
  camera = result;
  return result;
}

void DefaultCamera::OnInit(Scene * scene) {
    SceneObject::OnInit(scene);

  const auto inputManager = GetInput().lock();

  AddCleanup(inputManager->BindKey(window::EKey::Key_W, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputForward += 1;
    log::engine->Info("W Pressed");
                                     return true;
                                   }, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     log::engine->Info("W Released");
                                     inputForward -= 1;
                                     return true;
                                   }));

  AddCleanup(inputManager->BindKey(window::EKey::Key_S, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputForward -= 1;
                                     return true;
                                   }, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputForward += 1;
                                     return true;
                                   }));

  AddCleanup(inputManager->BindKey(window::EKey::Key_A, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputRight -= 1;
                                     return true;
                                   }, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputRight += 1;
                                     return true;
                                   }));
  AddCleanup(inputManager->BindKey(window::EKey::Key_D, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputRight += 1;
                                     return true;
                                   }, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     inputRight -= 1;
                                     return true;
                                   }));

  AddCleanup(inputManager->BindKey(window::EKey::Key_Space,
                                   [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     bWantsToGoUp = true;
                                     return true;
                                   }, [this](const std::shared_ptr<input::KeyInputEvent> &e) {
                                     bWantsToGoUp = false;
                                     return true;
                                   }));

  this->SetWorldLocation({0, 0, -10.f});

  constexpr float mouseInputScale = 20.0f;
  AddCleanup(inputManager->BindAxis(input::MouseX,
                                  [this](const std::shared_ptr<input::AxisInputEvent> &e) {
                                    const auto axisValue = e->GetValue();
                                    
                                    yaw += axisValue * mouseInputScale;// * GetEngine()->GetDeltaSeconds();
                                    UpdateRotation();
                                    return true;
                                  }));

  AddCleanup(inputManager->BindAxis(input::MouseY,
                                  [this](const std::shared_ptr<input::AxisInputEvent> &e) {
                                    const auto axisValue = e->GetValue();
                                    
                                    pitch += axisValue * mouseInputScale;// * GetEngine()->GetDeltaSeconds();
                                    pitch = std::clamp(pitch,-89.0f,89.0f);
                                    UpdateRotation();
                                    return true;
                                  }));
}

void DefaultCamera::Tick(float deltaTime) {
  SceneObject::Tick(deltaTime);

  constexpr auto moveSpeed = 60.f;
  const auto forwardDelta = GetWorldRotation().Forward() * inputForward *
                            moveSpeed * deltaTime;
  const auto rightDelta = GetWorldRotation().Right() * inputRight * moveSpeed *
                          deltaTime;
  const math::Vec3<> upDelta = bWantsToGoUp
                                 ? math::VECTOR_UP * moveSpeed * deltaTime
                                 : math::VECTOR_ZERO;
  const math::Vec3<> deltaFinal = forwardDelta + rightDelta + upDelta;
  const auto worldLocation = GetWorldLocation();
 SetWorldLocation(worldLocation + deltaFinal);
}

void DefaultCamera::UpdateRotation() {
  SetWorldRotation(math::QUAT_ZERO.ApplyYaw(yaw).ApplyPitch(pitch));
  //setWorldRotation( glm::mat4(glm::angleAxis(glm::radians(yaw),math::Vec3<>::Up)) * glm::mat4(glm::angleAxis(glm::radians(pitch),math::Vec3<>::Right)));
}
}
