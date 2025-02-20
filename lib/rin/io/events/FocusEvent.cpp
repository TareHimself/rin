#include "rin/io/events/FocusEvent.h"

namespace rin::io
{

    FocusEvent::FocusEvent(Window* inWindow, bool inFocused): Event(inWindow), focused(inFocused)
    {
       
    }
}
