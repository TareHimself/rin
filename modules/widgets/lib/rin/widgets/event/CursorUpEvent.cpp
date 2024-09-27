#include "rin/widgets/event/CursorUpEvent.hpp"

CursorUpEvent::CursorUpEvent(const Shared<WidgetSurface>& inSurface, const CursorButton inButton,
        const Vec2<float>& inPosition) : Event(inSurface), button(inButton), position(inPosition)
{
        
}
