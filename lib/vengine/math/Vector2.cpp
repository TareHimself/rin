#include <vengine/math/Vector2.hpp>
#include <cmath>
#include <glm/common.hpp>

namespace vengine::math {

Vector2::operator vk::Extent2D() const {
  return {static_cast<uint32_t>(std::round(x)), static_cast<uint32_t>(std::round(x))};
}

}
