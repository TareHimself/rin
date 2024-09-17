#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/window/CursorButton.hpp"

namespace aerox::widgets
{
    class CursorUpEvent : public Event
    {
    public:
        window::CursorButton button;
        Vec2<float> position;
        CursorUpEvent(const Shared<Surface>& inSurface,window::CursorButton inButton,const Vec2<float>& inPosition);
    };
}
