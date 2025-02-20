#include "rin/gui/events/CharacterEvent.h"
namespace rin::gui
{
    CharacterEvent::CharacterEvent(Surface* inSurface, const char& inCharacter, const Flags<io::InputModifier>& inModifiers) : Event(inSurface), character(inCharacter), modifiers(inModifiers)
    {
    }
}
