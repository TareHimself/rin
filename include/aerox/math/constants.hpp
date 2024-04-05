#pragma once
#include "Quat.hpp"
#include "Vec3.hpp"


namespace aerox::math {
inline Vec3<float> VECTOR_ZERO = {0, 0, 0};
inline Vec3<float> VECTOR_UP = {0, 1, 0};
inline Vec3<float> VECTOR_FORWARD = {0, 0, 1};
inline Vec3<float> VECTOR_RIGHT = {1, 0, 0};
inline Vec3<float> VECTOR_UNIT = {1, 1, 1};
inline Quat QUAT_ZERO = {0,VECTOR_FORWARD};
}
