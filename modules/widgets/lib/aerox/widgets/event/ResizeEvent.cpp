#include "aerox/widgets/event/ResizeEvent.hpp"

namespace aerox::widgets
{
    ResizeEvent::ResizeEvent(const Shared<Surface>& inSurface, const Vec2<float>& inSize) : Event(inSurface), size(inSize)
    {
        
    }
}
