#include "rin/widgets/event/CursorDownEvent.hpp"

CursorDownEvent::CursorDownEvent(const Shared<WidgetSurface>& inSurface, const CursorButton inButton,
        const Vec2<float>& inPosition) : Event(inSurface), button(inButton), position(inPosition)
{
        
}
