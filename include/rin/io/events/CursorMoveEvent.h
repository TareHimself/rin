#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
    struct CursorMoveEvent : Event
        {
            Vec2<> position{};
            CursorMoveEvent(Window* inWindow,const Vec2<>& inPosition);
        };
}
