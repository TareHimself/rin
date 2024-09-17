#include "aerox/widgets/event/CursorDownEvent.hpp"

namespace aerox::widgets
{
    CursorDownEvent::CursorDownEvent(const Shared<Surface>& inSurface, const window::CursorButton inButton,
        const Vec2<float>& inPosition) : Event(inSurface), button(inButton), position(inPosition)
    {
        
    }
}
