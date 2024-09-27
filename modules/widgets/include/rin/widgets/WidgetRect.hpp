#pragma once
#include "rin/core/math/Vec2.hpp"

struct WidgetRect
{
    Vec2<float> offset;
    Vec2<float> size;
    WidgetRect(const Vec2<float>& inOffset,const Vec2<float>& inSize);
    bool Contains(const Vec2<float>& point) const;
    bool IntersectsWith(const WidgetRect& other) const;
};
