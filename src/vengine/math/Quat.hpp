#pragma once
#include "vengine/containers/Array.hpp"

#include <ostream>
#include<glm/glm.hpp>
#include<glm/gtc/quaternion.hpp>

namespace vengine::math {
class Vector;
}

namespace vengine::math {
class Quat : public glm::quat {

  
public:
  using glm::quat::quat;
  // Quaternion();
  //
  // Quaternion(const float x_, const float y_, const float z_, const float w_);
  
  friend std::ostream& operator<<(std::ostream& os,const Quat& other) {
    os << other.x << " " << other.y << " " << other.z << " " << other.w;
    
    return os;
  }

  Quat(const glm::highp_quat& other);
  
  Quat& operator =(const glm::highp_quat& other);
  
  glm::mat4 Matrix() const {
    return glm::mat4_cast(*this);
  }

  Vector Forward() const;
  Vector Right() const;
  Vector Up() const;

  Quat ApplyPitch(float delta) const;
  Quat ApplyRoll(float delta) const;
  Quat ApplyYaw(float delta) const;
  
  Quat ApplyLocalPitch(float delta) const;
  Quat ApplyLocalRoll(float delta) const;
  Quat ApplyLocalYaw(float delta) const;
};
}

