#pragma once
#include "Event.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/core/math/Vec2.hpp"

class ResizeEvent : public Event
{
public:
    Vec2<float> size;
    ResizeEvent(const Shared<WidgetSurface>& inSurface, const Vec2<float>& inSize);
};
