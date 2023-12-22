#include "Vector3.hpp"


namespace vengine {

Vector3::Vector3() : Vector2() {
  z = 0.0f;
}

Vector3::Vector3(const float x_, const float y_,const float z_) : Vector2(x_,y_) {
  z = z_;
}

Vector3 Vector3::operator+(Vector3 const &other) const  {
  return {x + other.x,y + other.y,z + other.z};
}

Vector3 Vector3::operator-(Vector3 const &other) const {
  return {x - other.x,y - other.y,z - other.z};
}

Vector3 Vector3::operator*(Vector3 const &other) const {
  return {x * other.x,y * other.y,z * other.z};
}

Vector3 Vector3::operator/(Vector3 const &other) const {
  return {x / other.x,y / other.y,z / other.z};
}

Vector3 Vector3::operator+(float const &other) const {
  return {x + other,y + other,z + other};
}

Vector3 Vector3::operator-(float const &other) const {
  return {x - other,y - other,z - other};
}

Vector3 Vector3::operator*(float const &other) const {
  return {x * other,y * other,z * other};
}

Vector3 Vector3::operator/(float const &other) const {
  return {x / other,y / other,z / other};
}

bool Vector3::operator==(Vector3 const &other) const {
  return x == other.x && y == other.y && z == other.z;
}

bool Vector3::operator!=(Vector3 const &other) const {
  return !(*this == other);
}

}
