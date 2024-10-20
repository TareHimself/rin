#pragma once
#include "QuadInfo.hpp"
#include "rin/core/math/Matrix4.hpp"
#include "rin/core/math/Vec4.hpp"

inline int RIN_WIDGET_MAX_BATCH = 64;

struct WidgetBatchInfoPushConstant
{
    Vec4<float> viewport;
    Matrix4<float> projection;
};
