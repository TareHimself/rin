#pragma once
#include <ostream>
#include<glm/glm.hpp>
#include<glm/gtc/quaternion.hpp>

namespace vengine {
namespace math {
class Quaternion : public glm::quat {

  
public:
  using glm::quat::quat;
  // Quaternion();
  //
  // Quaternion(const float x_, const float y_, const float z_, const float w_);
  
  friend std::ostream& operator<<(std::ostream& os,const Quaternion& other) {
    os << "{ x:" << other.x << " y:" << other.y << " z:" << other.z << " w:" << other.w << " }";
    
    return os;
  }

  Quaternion& operator =(const glm::highp_quat& other) {
    x = other.x;
    y = other.y;
    z = other.z;
    w = other.w;
    return *this;
  }

  glm::mat4 matrix() const {
    return glm::mat4_cast(*this);
  }
};

const Quaternion ZeroQuat = {1.0f,0.0f,0.0f,0.0f};
}
}

