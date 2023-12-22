#pragma once
#include "Quat.hpp"
#include "Vector3.hpp"

namespace vengine {
class Transform {
public:
  Vector3 Position;
  Quat Rotation;
  Vector3 Scale;
};
}
