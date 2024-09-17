#pragma once
#include "aerox/core/math/Matrix3.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/core/math/Vec4.hpp"

namespace aerox::widgets
{
    struct QuadInfo
    {
        int textureId = -1;
        int samplerId = -1;
        Vec4<float> color{0.0f};
        Vec4<float> borderRadius{0.0f};
        Vec2<float> size{0.0f};
        Matrix3<float> transform{1.0f};
    };
}
