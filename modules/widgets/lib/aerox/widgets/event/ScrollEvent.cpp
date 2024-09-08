#include "aerox/widgets/event/ScrollEvent.hpp"

namespace aerox::widgets
{
    ScrollEvent::ScrollEvent(const Shared<Surface>& inSurface, const Vec2<float>& inDelta,
        const Vec2<float>& inPosition) : Event(inSurface), delta(inDelta), position(inPosition)
    {
    }
}
