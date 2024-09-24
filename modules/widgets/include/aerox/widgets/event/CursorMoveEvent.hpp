#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"

class CursorMoveEvent : public Event
{
public:
    Vec2<float> position;
    CursorMoveEvent(const Shared<WidgetSurface>& inSurface,const Vec2<float>& inPosition);
};
