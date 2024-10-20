#pragma once
#include <string>

#include "rin/core/math/Vec4.hpp"

class USDFItem
{
public:
    std::string id{};
    int x{0};
    int y{0};
    int width{0};
    int height{0};
    int atlas{0};
    Vec4<float> uv{0.0f};
};
