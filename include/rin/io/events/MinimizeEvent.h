#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
    struct MinimizeEvent : Event
        {
            MinimizeEvent(Window* inWindow);
        };
}
