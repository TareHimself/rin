#pragma once
#include "rin/core/math/Mat3.h"
namespace rin::gui
{
    struct StencilClip
    {
        Mat3<> transform;
        Vec2<> size;
    };
}
