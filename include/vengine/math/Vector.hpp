#pragma once

#include "Vector2.hpp"

#include <ostream>
#include <glm/glm.hpp>
#include <glm/ext/matrix_transform.hpp>

namespace vengine::math {

class Vector : public glm::vec3 {

public:
  using glm::vec3::vec3;
  
  friend std::ostream& operator<<(std::ostream& os,const Vector& other) {
    os << static_cast<std::string>(other);
    
    return os;
  }

  Vector(const glm::highp_vec3& other);
  Vector& operator =(const glm::highp_vec3& other);
  
  glm::mat4 TranslationMatrix() const;

  glm::mat4 ScaleMatrix() const;

  Vector();

  operator std::string() const;

  operator glm::highp_vec3() const;
};


}
