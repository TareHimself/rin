#pragma once
#include "rin/core/math/Mat3.h"
namespace rin::gui
{
    class DrawCommands
    {
    public:

        DrawCommands& Add(const Mat3<>& transform,const Vec2<>& size);
        DrawCommands& PushClip(const Mat3<>& transform,const Vec2<>& size);
        DrawCommands& PopClip();
    };
}
