#pragma once
#include "Quat.hpp"
#include "Vec3.hpp"

#include <ostream>

namespace aerox::math {
class Transform {
public:
  Vec3<> location{};
  Quat rotation;
  Vec3<> scale{};
  Transform();
  Transform(const glm::mat4& mat);
  Transform(const Vec3<>& _loc,const Quat& _rot,const Vec3<>& _scale);

  friend std::ostream& operator<<(std::ostream& os,const Transform& other) {
    os << "Location: " << other.location << std::endl;
    os << "Rotation: " << other.rotation << std::endl;
    os << "Scale: " << other.scale << std::endl;
    return os;
  }

public:
  [[nodiscard]] Transform RelativeTo(const Transform& other) const;

    [[nodiscard]] glm::mat4 GetLocationMatrix() const;

    [[nodiscard]] glm::mat4 GetRotationMatrix() const;


  [[nodiscard]] glm::mat4 Matrix() const;
};
}
