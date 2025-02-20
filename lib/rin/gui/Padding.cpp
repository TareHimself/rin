#include "rin/gui/Padding.h"
namespace rin::gui
{
    Padding::operator Vec4<float>() const
    {
        return Vec4{left,top,right,bottom};
    }
    Padding::operator Vec2<>() const
    {
        return Vec2<>{left + right,top + bottom};
    }
    Padding::Padding(const float inAll)
    {
        left = inAll;
        top = inAll;
        right = inAll;
        bottom = inAll;
    }
    Padding::Padding(const float inHorizontal, const float inVertical)
    {
        left = inHorizontal;
        top = inVertical;
        right = inHorizontal;
        bottom = inVertical;
    }
    Padding::Padding(const float inLeft, const float inRight, const float inTop, const float inBottom)
    {
        left = inLeft;
        top = inTop;
        right = inRight;
        bottom = inBottom;
    }
}
