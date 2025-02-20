#include "rin/io/events/KeyEvent.h"
namespace rin::io
{


    KeyEvent::KeyEvent(Window* inWindow, const InputKey& inKey, const InputState& inState, const Flags<InputModifier>& inModifiers) : Event(inWindow), key(inKey), state(inState),
        modifiers(inModifiers)
    {

    }
}
