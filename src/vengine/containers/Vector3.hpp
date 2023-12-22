#pragma once

#include "Vector2.hpp"

#include <glm/glm.hpp>

namespace vengine {

class Vector3 : public Vector2 {
  
public:
  float z = 0.0f;

  Vector3();
  Vector3(const float x_, const float y_,const float z_);

  // Arithmetic Operators
  Vector3 operator+(Vector3 const& other) const;
  Vector3 operator-(Vector3 const& other) const;
  Vector3 operator*(Vector3 const& other) const;
  Vector3 operator/(Vector3 const& other) const;

  Vector3 operator+(float const& other) const;
  Vector3 operator-(float const& other) const;
  Vector3 operator*(float const& other) const;
  Vector3 operator/(float const& other) const;

  bool operator==(Vector3 const& other) const;

  bool operator!=(Vector3 const& other) const;
};
}
