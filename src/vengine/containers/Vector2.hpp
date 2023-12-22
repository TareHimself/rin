#pragma once
#include <vulkan/vulkan.hpp>

namespace vengine {
class Vector2 {
public:
  float x = 0.0f;
  float y = 0.0f;

  Vector2();
  Vector2(const float x_, const float y_);

  // Arithmetic Operators
  Vector2 operator+(Vector2 const& other) const;
  Vector2 operator-(Vector2 const& other) const;
  Vector2 operator*(Vector2 const& other) const;
  Vector2 operator/(Vector2 const& other) const;

  Vector2 operator+(float const& other) const;
  Vector2 operator-(float const& other) const;
  Vector2 operator*(float const& other) const;
  Vector2 operator/(float const& other) const;

  operator vk::Extent2D() const;

  bool operator==(Vector2 const& other) const;

  bool operator!=(Vector2 const& other) const;
};
}
