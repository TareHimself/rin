#pragma once
#include <glm/vec2.hpp>
#include <ostream>
#include <vulkan/vulkan.hpp>

namespace vengine::math {
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

  explicit operator vk::Extent2D() const;

};
}
