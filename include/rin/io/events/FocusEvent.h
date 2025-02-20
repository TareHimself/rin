#pragma once
#include "Event.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
namespace rin::io
{
    struct FocusEvent : Event
        {
            bool focused;
            FocusEvent(Window* inWindow,bool inFocused);
        };
}
