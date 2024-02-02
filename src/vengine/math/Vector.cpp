#include <vengine/math/Vector.hpp>


namespace vengine::math {


Vector::Vector(const glm::highp_vec3 &other) {
  x = other.x;
  y = other.y;
  z = other.z;
}

Vector & Vector::operator=(const glm::highp_vec3 &other) {
  x = other.x;
  y = other.y;
  z = other.z;
  return *this;
}

glm::mat4 Vector::TranslationMatrix() const {
  return glm::translate(glm::mat4(1.0f),*this);
}

glm::mat4 Vector::ScaleMatrix() const {
  return glm::scale(glm::mat4(1.0f),*this);
}

Vector::Vector() {
  x = 0;
  y = 0;
  z = 0;
}

Vector::operator std::string() const {
  return std::to_string(x) + " " + std::to_string(y) + " " + std::to_string(z);
}

Vector::operator vec<3, float, glm::packed_highp>() const {
  return {this->x,this->y,this->z};
}

// Vector Vector::Zero = {0,0,0};
// Vector Vector::Up = {0,1,0};
// Vector Vector::Forward = {0,0,1};
// Vector Vector::Right = {1,0,0};
// Vector Vector::Unit = {1,1,1};
}
