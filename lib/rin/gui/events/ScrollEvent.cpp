#include "rin/gui/events/ScrollEvent.h"
namespace rin::gui
{
    
    ScrollEvent::ScrollEvent(Surface* inSurface, const Vec2<>& inDelta) : Event(inSurface), delta(inDelta)
    {
        
    }
}
