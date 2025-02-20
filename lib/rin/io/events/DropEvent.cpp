#include "rin/io/events/DropEvent.h"
namespace rin::io
{
    DropEvent::DropEvent(Window* inWindow, const Vec2<>& inPosition) : Event(inWindow), position(inPosition)
    {
    }
}
