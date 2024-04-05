#include <aerox/scene/components/CameraComponent.hpp>
#include "aerox/scene/Scene.hpp"
#include "aerox/scene/objects/SceneObject.hpp"
//#include <glm/gtx/transform.hpp>
#include <glm/ext/matrix_transform.hpp>

namespace aerox::scene {

glm::mat4 CameraComponent::GetViewMatrix() const {
  const auto transform = GetWorldTransform();
  return inverse(transform.GetLocationMatrix() * transform.GetRotationMatrix());
}

glm::mat4 CameraComponent::GetProjection(const float aspectRatio) const {
  return glm::rotate(glm::perspective(glm::radians(fieldOfView), aspectRatio,  nearClipPlane,farClipPlane),glm::radians(180.f),{1,0,0});
}

void CameraComponent::Draw(
    drawing::SceneFrameData *frameData, const math::Transform &parentTransform) {
  
}

void CameraComponent::SetRelativeTransform(const math::Transform &val) {
  RenderedComponent::SetRelativeTransform(val);
  
}
}
