#include "Vector2.hpp"
#include <cmath>
#include <glm/common.hpp>

namespace vengine {
namespace math {
// Vector2::Vector2() : glm::vec2() {
// }
//
// Vector2::Vector2(const float x_, const float y_) : glm::vec2(x_,y_){
// }

// Vector2::Vector2() {
//   x = 0.0f;
//   y = 0.0f;
// }
//
// Vector2::Vector2(const float x_, const float y_) {
//   x = x_;
//   y = x_;
// }
//
// Vector2 Vector2::operator+(Vector2 const &other) const  {
//   return {x + other.x,y + other.y};
// }
//
// Vector2 Vector2::operator-(Vector2 const &other) const {
//   return {x - other.x,y - other.y};
// }
//
// Vector2 Vector2::operator*(Vector2 const &other) const {
//   return {x * other.x,y * other.y};
// }
//
// Vector2 Vector2::operator/(Vector2 const &other) const {
//   return {x / other.x,y / other.y};
// }
//
// Vector2 Vector2::operator+(float const &other) const {
//   return {x + other,y + other};
// }
//
// Vector2 Vector2::operator-(float const &other) const {
//   return {x - other,y - other};
// }
//
// Vector2 Vector2::operator*(float const &other) const {
//   return {x * other,y * other};
// }
//
// Vector2 Vector2::operator/(float const &other) const {
//   return {x / other,y / other};
// }
//
Vector2::operator vk::Extent2D() const {
  return {static_cast<uint32_t>(std::round(x)), static_cast<uint32_t>(std::round(x))};
}
//
// bool Vector2::operator==(Vector2 const &other) const {
//   return x == other.x && y == other.y;
// }
//
// bool Vector2::operator!=(Vector2 const &other) const {
//   return !(*this == other);
// }
}
}
