#include "rin/widgets/event/ResizeEvent.hpp"

ResizeEvent::ResizeEvent(const Shared<WidgetSurface>& inSurface, const Vec2<float>& inSize) : Event(inSurface), size(inSize)
{
        
}