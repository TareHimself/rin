#include "rin/io/events/ResizeEvent.h"

namespace rin::io
{

    ResizeEvent::ResizeEvent(Window* inWindow, const Vec2<int>& inSize): Event(inWindow), size(inSize)
    {
    }
}
