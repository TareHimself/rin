#include "aerox/math/utils.hpp"

namespace aerox::math {

float mapRange(const float a, const float iMin, const float iMax, const float oMin, const float oMax) {
  const float normal = (a - iMin) / (iMax - iMin);

  return oMin * (1.0f - normal) + oMax * normal;
}
}
