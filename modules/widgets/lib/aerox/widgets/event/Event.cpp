#include "aerox/widgets/event/Event.hpp"

namespace aerox::widgets
{
    Event::Event(const Shared<Surface>& inSurface)
    {
        surface = inSurface;
    }
}
