#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
    struct DropEvent : Event
        {
        Vec2<> position{};
            DropEvent(Window* inWindow,const Vec2<>& inPosition);
        };
}
