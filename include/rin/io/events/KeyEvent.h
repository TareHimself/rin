#pragma once
#include "Event.h"
#include "rin/core/Flags.h"
#include "rin/core/math/Vec4.h"
#include "rin/gui/graphics/Surface.h"
#include "rin/io/InputKey.h"
#include "rin/io/InputModifier.h"
#include "rin/io/InputState.h"
namespace rin::io
{
    struct KeyEvent : Event
    {
        InputKey key;
        InputState state;
        Flags<InputModifier> modifiers;
        KeyEvent(Window* inWindow, const InputKey& inKey, const InputState& inState, const Flags<InputModifier>& inModifiers);
    };
}
