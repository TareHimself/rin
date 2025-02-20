#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
   struct ScrollEvent : Event
        {
            Vec2<> delta{0};
            ScrollEvent(Window* inWindow,const Vec2<>& inDelta);
        };
}
