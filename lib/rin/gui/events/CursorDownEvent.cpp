#include "rin/gui/events/CursorDownEvent.h"
namespace rin::gui
{

    CursorDownEvent::CursorDownEvent(Surface* inSurface, const io::CursorButton& inButton, const io::InputState& inState, const Vec2<>& inPosition,
        const Flags<io::InputModifier>& inModifiers) : CursorButtonEvent(inSurface, inButton, inState, inPosition, inModifiers)
    {
    }
}
