#pragma once
#include "Quat.hpp"
#include "Vector.hpp"


namespace vengine::math {
inline Vector VECTOR_ZERO = {0,0,0};
inline Vector VECTOR_UP = {0,1,0};
inline Vector VECTOR_FORWARD = {0,0,1};
inline Vector VECTOR_RIGHT = {1,0,0};
inline Vector VECTOR_UNIT = {1,1,1};
inline Quat QUAT_ZERO = glm::angleAxis(glm::radians(0.f),VECTOR_FORWARD);
}
