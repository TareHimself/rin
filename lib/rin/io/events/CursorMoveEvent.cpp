#include "rin/io/events/CursorMoveEvent.h"

namespace rin::io
{
    
    CursorMoveEvent::CursorMoveEvent(Window* inWindow, const Vec2<>& inPosition) : Event(inWindow), position(inPosition)
    {
    }
}
