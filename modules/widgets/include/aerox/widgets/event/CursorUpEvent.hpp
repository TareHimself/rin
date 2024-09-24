#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/window/CursorButton.hpp"

class CursorUpEvent : public Event
{
public:
    CursorButton button;
    Vec2<float> position;
    CursorUpEvent(const Shared<WidgetSurface>& inSurface,CursorButton inButton,const Vec2<float>& inPosition);
};
