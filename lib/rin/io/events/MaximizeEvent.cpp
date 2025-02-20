#include "rin/io/events/MaximizeEvent.h"

namespace rin::io
{

    MaximizeEvent::MaximizeEvent(Window* inWindow, bool inMaximized): Event(inWindow), maximized(inMaximized)
    {
    }
}
