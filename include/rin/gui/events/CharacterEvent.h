#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/io/InputModifier.h"
namespace rin::gui
{
    struct CharacterEvent : Event
    {
        char character;
        Flags<io::InputModifier> modifiers{};
        CharacterEvent(Surface * inSurface,const char& inCharacter,const Flags<io::InputModifier>& inModifiers);
    };
}
