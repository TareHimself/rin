#include "aerox/core/math/Quat.hpp"
namespace aerox
{
    Quat::Quat() : Quat(0,0,0,0)
    {

    }

    Quat::Quat(const float inX, const float inY, const float inZ, const float inW): x(inX), y(inY), z(inZ), w(inW)
    {
        
    }
}

