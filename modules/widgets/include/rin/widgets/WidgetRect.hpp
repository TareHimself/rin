#pragma once
#include "rin/core/math/Vec2.hpp"

struct WidgetRect
{
    Vec2<float> offset;
    Vec2<float> size;
    WidgetRect(const Vec2<float>& inOffset, const Vec2<float>& inSize);
    [[nodiscard]] bool Contains(const Vec2<float>& point) const;
    [[nodiscard]] bool IntersectsWith(const WidgetRect& other) const;
};
