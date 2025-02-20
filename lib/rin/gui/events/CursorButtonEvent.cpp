#include "rin/gui/events/CursorButtonEvent.h"
namespace rin::gui
{
    CursorButtonEvent::CursorButtonEvent(Surface* inSurface, const io::CursorButton& inButton, const io::InputState& inState, const Vec2<>& inPosition,
        const Flags<io::InputModifier>& inModifiers)   : Event(inSurface), button(inButton), state(inState), position(inPosition), modifiers(inModifiers)
    {
    }
}
