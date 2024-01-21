#include "Quat.hpp"

#include "Vector.hpp"
#include "constants.hpp"


namespace vengine::math {

Quat::Quat(const glm::highp_quat &other) {
  x = other.x;
  y = other.y;
  z = other.z;
  w = other.w;
}

Quat & Quat::operator=(const glm::highp_quat &other) {
  x = other.x;
  y = other.y;
  z = other.z;
  w = other.w;
  return *this;
}


Vector Quat::Forward() const {
  return *this * VECTOR_FORWARD;
}

Vector Quat::Right() const {
  return *this * VECTOR_RIGHT;
}

Vector Quat::Up() const {
  return *this * VECTOR_UP;
}

Quat Quat::ApplyPitch(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),VECTOR_RIGHT);
}

Quat Quat::ApplyRoll(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),VECTOR_FORWARD);
}

Quat Quat::ApplyYaw(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),VECTOR_UP);
}

Quat Quat::ApplyLocalPitch(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),Right());
}

Quat Quat::ApplyLocalRoll(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),Forward());
}

Quat Quat::ApplyLocalYaw(float delta) const {
  return *this * glm::angleAxis(glm::radians(delta),Up());
}
}
