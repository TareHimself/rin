#pragma once
#include "rin/core/math/Vec2.h"
#include "rin/core/math/Vec4.h"
namespace rin::gui
{
    struct Padding
    {
        float left;
        float right;
        float top;
        float bottom;

        explicit operator Vec4<float>() const;
        explicit operator Vec2<float>() const;
        Padding(float inAll);
        Padding(float inHorizontal,float inVertical);
        Padding(float inLeft,float inRight,float inTop,float inBottom);
    };
}
