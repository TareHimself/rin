#include "rin/io/events/CharacterEvent.h"

namespace rin::io
{
    
    CharacterEvent::CharacterEvent(Window* inWindow, const char inCharacter, const Flags<InputModifier>& inModifiers) : Event(inWindow), character(inCharacter), modifiers(inModifiers)
    {
        
    }
}
