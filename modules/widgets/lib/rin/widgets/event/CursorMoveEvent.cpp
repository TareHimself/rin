#include "rin/widgets/event/CursorMoveEvent.hpp"

CursorMoveEvent::CursorMoveEvent(const Shared<WidgetSurface>& inSurface,
                                 const Vec2<float>& inPosition) : Event(inSurface), position(inPosition)
{
}
