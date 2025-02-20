#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
#include "rin/io/InputModifier.h"
namespace rin::io
{
    struct CharacterEvent : Event
        {
            char character;
            Flags<InputModifier> modifiers{};
            CharacterEvent(Window* inWindow, const char inCharacter,const Flags<InputModifier>& inModifiers);
        };
}
