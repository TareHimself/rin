#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/window/ECursorButton.hpp"

namespace aerox::widgets
{
    class CursorUpEvent : public Event
    {
    public:
        window::ECursorButton button;
        Vec2<float> position;
        CursorUpEvent(const Shared<Surface>& inSurface,window::ECursorButton inButton,const Vec2<float>& inPosition);
    };
}
