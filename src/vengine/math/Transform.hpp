#pragma once
#include "Quaternion.hpp"
#include "Vector3.hpp"

#include <ostream>

namespace vengine {
namespace math {
class Transform {
public:
  Vector3 location;
  Quaternion rotation;
  Vector3 scale;
  Transform(Vector3 _loc = ZeroVector3, Quaternion _rot = ZeroQuat, Vector3 _scale = {1.f,1.0f,1.0f});

  friend std::ostream& operator<<(std::ostream& os,const Transform& other) {
    os << "Location: " << other.location << std::endl;
    os << "Rotation: " << other.rotation << std::endl;
    os << "Scale: " << other.scale << std::endl;
    return os;
  }

public:
  Transform relativeTo(const Transform& other) const;

  glm::mat4 toMatrix() const;
};
}
}
