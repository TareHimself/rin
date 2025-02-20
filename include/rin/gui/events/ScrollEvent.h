#pragma once
#include "Event.h"
#include "rin/io/events/Event.h"
namespace rin::gui
{
    struct ScrollEvent : Event
    {
        Vec2<> delta{0};
        ScrollEvent(Surface * inSurface,const Vec2<>& inDelta);
    };
}
