#pragma once

#include "Vector2.hpp"

#include <ostream>
#include <glm/glm.hpp>
#include <glm/ext/matrix_transform.hpp>

namespace vengine {
namespace math {
typedef glm::vec3 glmBaseType;
class Vector3 : public glm::vec3 {

public:
  using glm::vec3::vec3;
  // Vector3();
  //
  // Vector3(const float x_, const float y_, const float z_);

  
  friend std::ostream& operator<<(std::ostream& os,const Vector3& other) {
    os << "{ x:" << other.x << " y:" << other.y << " z:" << other.z << " }";
    
    return os;
  }

  glm::vec3 toGltfCoordinates() const;
  
  Vector3& operator =(const glm::highp_vec3& other);
  
  glm::mat4 translationMatrix() const;

  glm::mat4 scaleMatrix() const;
public:
  // glm::vec3 * glm() {
  //   return &static_cast<glm::vec3>(*this);
  // }
  //
  // Vector3& operator*=(Vector3 const& other);
  //
  // // Arithmetic Operators
  // Vector3 operator+(Vector3 const& other) const;
  // Vector3 operator-(Vector3 const& other) const;
  // Vector3 operator*(Vector3 const& other) const;
  // Vector3 operator/(Vector3 const& other) const;
  //
  // Vector3 operator+(float const& other) const;
  // Vector3 operator-(float const& other) const;
  // Vector3 operator*(float const& other) const;
  // Vector3 operator/(float const& other) const;
  //
  // bool operator==(Vector3 const& other) const;
  //
  // bool operator!=(Vector3 const& other) const;
};

const Vector3 ZeroVector3 = {0.0f,0.0f,0.0f};
}
}
