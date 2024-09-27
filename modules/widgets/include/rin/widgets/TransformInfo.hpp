#pragma once
#include "WidgetRect.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/core/math/Matrix3.hpp"
#include "rin/core/math/Vec2.hpp"

class WidgetSurface;
class WidgetContainerSlot;
class Widget;

struct TransformInfo
{
    Matrix3<float> transform{1.0f};
    Vec2<float> size{0.0f};
    int depth = 0;

    TransformInfo(const Matrix3<float>& inTransform,const Vec2<float>& inSize,int inDepth = 0);

    explicit TransformInfo(const WidgetSurface * inSurface);

    explicit TransformInfo(const Shared<WidgetSurface>& inSurface);

    explicit  TransformInfo(const Shared<Widget>& widget,bool absolute = false);

    TransformInfo AccountFor(const Shared<WidgetContainerSlot>& slot) const;
    TransformInfo AccountFor(const Shared<Widget>& widget) const;
    TransformInfo AccountFor(const Widget * widget) const;

    bool IsPointWithin(const Vec2<float>& point) const;

    WidgetRect ComputeAxisAlignedBoundingRect() const;
};
