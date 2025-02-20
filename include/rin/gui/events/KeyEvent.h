#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/io/CursorButton.h"
#include "rin/io/InputModifier.h"
#include "rin/io/InputState.h"
namespace rin::gui
{
    struct KeyEvent : Event
    {
        io::CursorButton button{};
        io::InputState state{};
        Flags<io::InputModifier> modifiers{};
        KeyEvent(Surface * inSurface,const io::CursorButton& inButton,const io::InputState& inState,const Flags<io::InputModifier>& inModifiers);
    };
}
