#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"

namespace aerox::widgets
{
    class ScrollEvent : public Event
    {
    public:
        Vec2<float> delta;
        Vec2<float> position;
        ScrollEvent(const Shared<Surface>& inSurface,const Vec2<float>& inDelta,const Vec2<float>& inPosition);
    };
}
