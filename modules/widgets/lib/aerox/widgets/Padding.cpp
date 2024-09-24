#include "aerox/widgets/Padding.hpp"
Padding::Padding(float inAll) : Padding(inAll,inAll)
{
        
}

Padding::Padding(float inHorizontal,float inVertical) : Padding(inHorizontal,inHorizontal,inVertical,inVertical)
{

}

Padding::Padding(float inLeft, float inRight,float inTop, float inBottom)
{
    left = inLeft;
    right = inRight;
    top = inTop;
    bottom = inBottom;
}
