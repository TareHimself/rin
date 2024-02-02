#include <vengine/scene/components/CameraComponent.hpp>
#include "vengine/scene/Scene.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include <glm/ext/matrix_transform.hpp>

namespace vengine::scene {

glm::mat4 CameraComponent::GetViewMatrix() const {
  const auto transform = GetWorldTransform();
  
  return inverse(transform.location.TranslationMatrix() * 
                   transform.rotation.Matrix());
}

glm::mat4 CameraComponent::GetProjection(const float aspectRatio) const {
  return glm::rotate(glm::perspective(glm::radians(fieldOfView), aspectRatio,  nearClipPlane,farClipPlane),glm::radians(180.f),{1,0,0});
}

void CameraComponent::Draw(drawing::SceneDrawer *drawer,
                           drawing::SimpleFrameData *frameData) {
  
}
}
