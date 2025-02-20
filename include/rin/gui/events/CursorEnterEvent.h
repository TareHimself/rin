#pragma once
#include "CursorMoveEvent.h"
#include "rin/core/math/Vec2.h"
#include <list>
namespace rin::gui
{
    class Widget;
    struct CursorEnterEvent : CursorMoveEvent
    {
        std::list<Widget*> hoverList{};
        CursorEnterEvent(Surface * inSurface,const Vec2<>& inPosition);
    };
}
