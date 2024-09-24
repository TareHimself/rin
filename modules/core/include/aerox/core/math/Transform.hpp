#pragma once
#include "Matrix4.hpp"
#include "Quat.hpp"
#include "Vec3.hpp"
struct Transform
{
    Vec3<float> location{0};
    Quat rotation{};
    Vec3<float> scale{0};
    Matrix4<float> ToMatrix();
};