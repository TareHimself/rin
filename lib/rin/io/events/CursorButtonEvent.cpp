#include "rin/io/events/CursorButtonEvent.h"

namespace rin::io
{
    CursorButtonEvent::CursorButtonEvent(Window* inWindow, const CursorButton& inButton, const InputState& inState, const Vec2<>& inPosition,
        const Flags<InputModifier>& inModifiers) : Event(inWindow), button(inButton), state(inState), position(inPosition), modifiers(inModifiers)
    {
    }
}
