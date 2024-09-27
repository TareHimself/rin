#pragma once
#include "Event.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/core/math/Vec2.hpp"

class ScrollEvent : public Event
{
public:
    Vec2<float> delta;
    Vec2<float> position;
    ScrollEvent(const Shared<WidgetSurface>& inSurface,const Vec2<float>& inDelta,const Vec2<float>& inPosition);
};
