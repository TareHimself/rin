#include "aerox/widgets/event/CursorUpEvent.hpp"

namespace aerox::widgets
{
    CursorUpEvent::CursorUpEvent(const Shared<Surface>& inSurface, const window::ECursorButton inButton,
        const Vec2<float>& inPosition) : Event(inSurface), button(inButton), position(inPosition)
    {
        
    }
}
