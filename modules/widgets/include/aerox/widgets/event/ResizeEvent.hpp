#pragma once
#include "Event.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Vec2.hpp"

namespace aerox::widgets
{
    class ResizeEvent : public Event
    {
    public:
        Vec2<float> size;
        ResizeEvent(const Shared<Surface>& inSurface,const Vec2<float>& inSize);
    };
}
