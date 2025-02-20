#pragma once
#include "Rect.h"
#include "rin/core/math/Mat3.h"
#include "rin/core/math/Vec4.h"
namespace rin::gui
{
    struct TransformInfo
    {
        Mat3<> transform;
        Rect clip{};
        TransformInfo(const Mat3<>& inTransform, const Rect& inClip);
    };
}
