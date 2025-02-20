#include "rin/gui/events/CursorEnterEvent.h"
namespace rin::gui
{

    CursorEnterEvent::CursorEnterEvent(Surface* inSurface, const Vec2<>& inPosition) : CursorMoveEvent(inSurface, inPosition)
    {
    }
}
