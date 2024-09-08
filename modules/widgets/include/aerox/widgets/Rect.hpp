#pragma once
#include "aerox/core/math/Vec2.hpp"

namespace aerox::widgets
{
    struct Rect
    {
        Vec2<float> offset;
        Vec2<float> size;
        Rect(const Vec2<float>& inOffset,const Vec2<float>& inSize);
        bool Contains(const Vec2<float>& point) const;
        bool IntersectsWith(const Rect& other) const;
    };
}
