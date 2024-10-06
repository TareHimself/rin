#pragma once
#include "rin/core/math/Matrix3.hpp"
#include "rin/core/math/Vec2.hpp"
#include "rin/core/math/Vec4.hpp"

struct QuadInfo
{
    // Options for this quad
    // x => the texture id or -1 for no texture
    // y => the render mode for this quad 0=normal 1=mtsdf
    // z => not used
    // w => not used
    Vec4<int> opts{-1,0,0,0};
    // The color of this quad or the tint for the texture
    Vec4<float> color{0.0f};
    // The border radius for this quad
    // x => top left
    // y => top right
    // z => bottom left
    // w => bottom right
    Vec4<float> borderRadius{0.0f};
    // The size of this quad
    Vec2<float> size{0.0f};
    // The transform to apply to this quad
    Matrix3<float> transform{1.0f};
    // The uv mapping for this quad
    // x => u begin
    // y => v begin
    // z => u end
    // w => v end
    Vec4<float> uv{0.0f,0.0f,1.0f,1.0f};
};
