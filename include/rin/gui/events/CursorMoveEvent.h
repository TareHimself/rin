#pragma once
#include "Event.h"
#include "rin/core/math/Vec2.h"
namespace rin::gui
{
    struct CursorMoveEvent : Event
    {
        Vec2<> position{};
        CursorMoveEvent(Surface * inSurface,const Vec2<>& inPosition);
    };
}
