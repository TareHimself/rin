#include "Vector3.hpp"


namespace vengine {
namespace math {
// Vector3::Vector3() : glm::vec3() {
// }
//
// Vector3::Vector3(const float x_, const float y_,const float z_) : glm::vec3(x_,y_,z_) {
//
// }

// Vector3 Vector3::operator+(Vector3 const &other) const  {
//   return {x + other.x,y + other.y,z + other.z};
// }
//
// Vector3 Vector3::operator-(Vector3 const &other) const {
//   return {x - other.x,y - other.y,z - other.z};
// }
//
// Vector3 Vector3::operator*(Vector3 const &other) const {
//   return {x * other.x,y * other.y,z * other.z};
// }
//
// Vector3 Vector3::operator/(Vector3 const &other) const {
//   return {x / other.x,y / other.y,z / other.z};
// }
//
// Vector3 Vector3::operator+(float const &other) const {
//   return {x + other,y + other,z + other};
// }
//
// Vector3 Vector3::operator-(float const &other) const {
//   return {x - other,y - other,z - other};
// }
//
// Vector3 Vector3::operator*(float const &other) const {
//   return {x * other,y * other,z * other};
// }
//
// Vector3 Vector3::operator/(float const &other) const {
//   return {x / other,y / other,z / other};
// }
//
// bool Vector3::operator==(Vector3 const &other) const {
//   return x == other.x && y == other.y && z == other.z;
// }
//
// bool Vector3::operator!=(Vector3 const &other) const {
//   return !(*this == other);
// }
// Vector3& Vector3::operator*=(Vector3 const &other) {
//   auto a = glm();
//   const auto b = other.glm();
//   a *= b;
//   return *this;
// }
glm::vec3 Vector3::toGltfCoordinates() const{
  return {this->y,this->z,this->x};
}

Vector3 & Vector3::operator=(const glm::highp_vec3 &other) {
  x = other.x;
  y = other.y;
  z = other.z;
  return *this;
}

glm::mat4 Vector3::translationMatrix() const {
  return glm::translate(glm::mat4(1.0f),this->toGltfCoordinates());
}

glm::mat4 Vector3::scaleMatrix() const {
  return glm::scale(glm::mat4(1.0f),this->toGltfCoordinates());
}
}
}
