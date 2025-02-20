#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/io/CursorButton.h"
#include "rin/io/InputModifier.h"
#include "rin/io/InputState.h"
namespace rin::io
{
    struct CursorButtonEvent : Event
    {
        CursorButton button{};
        InputState state{};
        Vec2<> position{};
        Flags<InputModifier> modifiers{};
        CursorButtonEvent(Window* inWindow,const CursorButton& inButton,const InputState& inState,const Vec2<>& inPosition,const Flags<InputModifier>& inModifiers);
    };
}
