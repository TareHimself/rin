#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
    struct ResizeEvent : Event
        {
            Vec2<int> size{0};
            ResizeEvent(Window* inWindow,const Vec2<int>& inSize);
        };
}
