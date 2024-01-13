#pragma once
#include <glm/vec2.hpp>
#include <ostream>
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace math {
class Vector2 : public glm::vec2 {
public:
  using glm::vec2::vec2;
  
  friend std::ostream& operator<<(std::ostream& os,const Vector2& other) {
    os << "{ x:" << other.x << " y:" << other.y <<  " }";
    
    return os;
  }


  Vector2& operator =(const glm::highp_vec2& other) {
    x = other.x;
    y = other.y;
    return *this;
  }
// public:
//
//   Vector2();
//
//   Vector2(const float x_, const float y_);
  // float x = 0.0f;
  // float y = 0.0f;
  //
  // Vector2();
  // Vector2(const float x_, const float y_);
  //
  // // Arithmetic Operators
  // Vector2 operator+(Vector2 const& other) const;
  // Vector2 operator-(Vector2 const& other) const;
  // Vector2 operator*(Vector2 const& other) const;
  // Vector2 operator/(Vector2 const& other) const;
  //
  // Vector2 operator+(float const& other) const;
  // Vector2 operator-(float const& other) const;
  // Vector2 operator*(float const& other) const;
  // Vector2 operator/(float const& other) const;
  //
  operator vk::Extent2D() const;
  //
  // bool operator==(Vector2 const& other) const;
  //
  // bool operator!=(Vector2 const& other) const;
  
};
const Vector2 ZeroVector2 = {0.0f,0.0f};
}
}
