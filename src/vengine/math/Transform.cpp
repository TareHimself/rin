#include "Transform.hpp"


namespace vengine {
namespace math {
Transform::Transform(Vector3 _loc, Quaternion _rot, Vector3 _scale) {
  location = _loc;
  rotation = _rot;
  scale = _scale;
}

Transform Transform::relativeTo(const Transform &other) const {
  Transform result = *this;
  result.scale = result.scale * other.scale;
  result.rotation = result.rotation * other.rotation;

  result.location.x = other.location.x + (location.x * rotation.w + location.y * rotation.z - location.z * rotation.y);
  result.location.y = other.location.y + (-location.x * rotation.z + location.y * rotation.w + location.z * rotation.x);
  result.location.z = other.location.z + (location.x * rotation.y - location.y * rotation.x + location.z * rotation.w);

  return result;
}

glm::mat4 Transform::toMatrix() const {
  // auto result = glm::mat4(1.0f);
  //
  // auto translationMatrix = glm::translate(result,location);
  // auto rotationMatrix = glm
  // result = glm::translate(result,location);
  // result *= glm::mat4_cast(rotation);
  // result = glm::scale(result,scale);

  return location.translationMatrix() * rotation.matrix() * scale.scaleMatrix();
}
}
}
