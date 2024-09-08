#include "aerox/widgets/event/CursorMoveEvent.hpp"

namespace aerox::widgets
{
    CursorMoveEvent::CursorMoveEvent(const Shared<Surface>& inSurface, const Vec2<float>& inPosition) : Event(inSurface), position(inPosition)
    {

    }
}
