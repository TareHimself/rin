#include "rin/io/events/ScrollEvent.h"

namespace rin::io
{

    ScrollEvent::ScrollEvent(Window* inWindow, const Vec2<>& inDelta): Event(inWindow), delta(inDelta)
    {

    }
}
