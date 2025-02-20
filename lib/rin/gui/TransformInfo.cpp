#include "rin/gui/TransformInfo.h"
namespace rin::gui
{
    TransformInfo::TransformInfo(const Mat3<>& inTransform, const Rect& inClip)
    {
        transform = inTransform;
        clip = inClip;
    }
}
