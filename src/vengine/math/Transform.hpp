#pragma once
#include "Quat.hpp"
#include "Vector.hpp"

#include <ostream>

namespace vengine::math {
class Transform {
public:
  Vector location;
  Quat rotation;
  Vector scale;
  Transform();
  Transform(const glm::mat4& mat);
  Transform(Vector _loc, Quat _rot, Vector _scale);

  friend std::ostream& operator<<(std::ostream& os,const Transform& other) {
    os << "Location: " << other.location << std::endl;
    os << "Rotation: " << other.rotation << std::endl;
    os << "Scale: " << other.scale << std::endl;
    return os;
  }

public:
  Transform RelativeTo(const Transform& other) const;

  glm::mat4 Matrix() const;
};
}
