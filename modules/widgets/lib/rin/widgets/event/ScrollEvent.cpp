#include "rin/widgets/event/ScrollEvent.hpp"

ScrollEvent::ScrollEvent(const Shared<WidgetSurface>& inSurface, const Vec2<float>& inDelta,
                         const Vec2<float>& inPosition) : Event(inSurface), delta(inDelta), position(inPosition)
{
}
