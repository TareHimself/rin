#include "Transform.hpp"

#include "constants.hpp"


namespace vengine::math {
Transform::Transform() : location(VECTOR_ZERO), rotation(QUAT_ZERO), scale(VECTOR_UNIT) {
}

Transform::Transform(const glm::mat4 &mat) {
  location = glm::vec3(mat[3]);
  rotation = Quat(glm::quat(glm::mat3(mat)));
  scale = glm::vec3(length(glm::vec3(mat[0])),length(glm::vec3(mat[1])),length(glm::vec3(mat[2])));
}

Transform::Transform(Vector _loc, Quat _rot, Vector _scale) : location(_loc), rotation(_rot), scale(_scale) {
  
}

Transform Transform::RelativeTo(const Transform &other) const {
  return inverse(other.Matrix()) * this->Matrix();
}

glm::mat4 Transform::Matrix() const {
  return location.TranslationMatrix() * rotation.Matrix() * scale.ScaleMatrix();
}
}
