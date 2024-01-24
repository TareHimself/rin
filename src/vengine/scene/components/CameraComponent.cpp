#include "CameraComponent.hpp"

#include "vengine/Engine.hpp"
#include "vengine/input/InputManager.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/scene/Scene.hpp"
#include "vengine/scene/SceneObject.hpp"

#include <glm/ext/matrix_transform.hpp>

namespace vengine::scene {

glm::mat4 CameraComponent::GetViewMatrix() const {
  const auto transform = GetWorldTransform();
  
  return inverse(transform.location.TranslationMatrix() * 
                   transform.rotation.Matrix());
}

glm::mat4 CameraComponent::GetProjection(float aspectRatio) const {
  return glm::rotate(glm::perspective(glm::radians(fieldOfView), aspectRatio,  nearClipPlane,farClipPlane),glm::radians(180.f),{1,0,0});
}

void CameraComponent::Draw(drawing::SceneDrawer *drawer,
                           drawing::SimpleFrameData *frameData) {
  
}
}
