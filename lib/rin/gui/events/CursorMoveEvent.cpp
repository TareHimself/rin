#include "rin/gui/events/CursorMoveEvent.h"
namespace rin::gui
{

    CursorMoveEvent::CursorMoveEvent(Surface* inSurface, const Vec2<>& inPosition) : Event(inSurface), position(inPosition)
    {
    }
}
