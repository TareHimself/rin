#pragma once
#include "Event.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/core/math/Vec2.hpp"
#include "rin/window/CursorButton.hpp"

class CursorDownEvent : public Event
{
public:
    CursorButton button;
    Vec2<float> position;
    CursorDownEvent(const Shared<WidgetSurface>& inSurface, CursorButton inButton, const Vec2<float>& inPosition);
};
