#pragma once
#include "QuadInfo.hpp"
#include "aerox/core/math/Matrix4.hpp"
#include "aerox/core/math/Vec4.hpp"

namespace aerox::widgets
{
    
    struct BatchInfo {
        static constexpr int MAX_BATCH = 64;
        Vec4<float> viewport;
        Matrix4<float> projection;
        QuadInfo quads[MAX_BATCH];
    };
}
