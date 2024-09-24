#pragma once
#include "aerox/widgets/WidgetRect.hpp"

WidgetRect::WidgetRect(const Vec2<float>& inOffset, const Vec2<float>& inSize) : offset(inOffset), size(inSize)
{

}

bool WidgetRect::Contains(const Vec2<float>& point) const
{
    return offset.x <= point.x && point.x <= offset.x + size.x && offset.y<= point.y && point.y <= offset.y + size.y;
}

bool WidgetRect::IntersectsWith(const WidgetRect& other) const
{
    auto a1 = offset;
    auto a2 = a1 + size;
    auto b1 = other.offset;
    auto b2 = b1 + other.size;

    if (a1.x <= b1.x)
    {
        if (a1.y <= b1.y)
            return b1.x <= a2.x && b1.y <= a2.y; // A top left B bottom right
        return b1.x <= a2.x && a1.y <= b2.y; // A Bottom left B Top right
    }

    if (a1.y <= b1.y)
        return a1.x <= b2.x && b1.y <= a2.y; // A top right B bottom left
    return a1.x <= b2.x && a1.y <= b2.y; // A bottom right B top left
}
