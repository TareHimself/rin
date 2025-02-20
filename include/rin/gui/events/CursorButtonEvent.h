#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/core/math/Vec2.h"
#include "rin/io/CursorButton.h"
#include "rin/io/InputModifier.h"
#include "rin/io/InputState.h"

namespace rin::gui
{
    class Widget;
    struct CursorButtonEvent : Event
    {
        io::CursorButton button{};
        io::InputState state{};
        Vec2<> position{};
        Flags<io::InputModifier> modifiers{};
        CursorButtonEvent(Surface * inSurface,const io::CursorButton& inButton,const io::InputState& inState,const Vec2<>& inPosition,const Flags<io::InputModifier>& inModifiers);
    };
}
