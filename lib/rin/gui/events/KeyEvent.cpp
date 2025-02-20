#include "rin/gui/events/KeyEvent.h"
namespace rin::gui
{
    KeyEvent::KeyEvent(Surface* inSurface, const io::CursorButton& inButton, const io::InputState& inState, const Flags<io::InputModifier>& inModifiers)  : Event(inSurface), button(inButton), state(inState), modifiers(inModifiers)
    {
        
    }
}
