#pragma once
#include "CursorButtonEvent.h"
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/core/math/Vec2.h"
#include "rin/io/CursorButton.h"
#include "rin/io/InputModifier.h"
#include "rin/io/InputState.h"

namespace rin::gui
{
    class Widget;
    struct CursorDownEvent : CursorButtonEvent
    {
        Widget * target{};
        CursorDownEvent(Surface * inSurface,const io::CursorButton& inButton,const io::InputState& inState,const Vec2<>& inPosition,const Flags<io::InputModifier>& inModifiers);
    };
}
