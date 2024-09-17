#pragma once
#include "Rect.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Matrix3.hpp"
#include "aerox/core/math/Vec2.hpp"

namespace aerox::widgets
{
    class Surface;
}

namespace aerox::widgets
{
    class ContainerSlot;
}

namespace aerox::widgets
{
    class Widget;
}

namespace aerox::widgets
{
    struct TransformInfo
    {
        Matrix3<float> transform{1.0f};
        Vec2<float> size{0.0f};
        int depth = 0;

        TransformInfo(const Matrix3<float>& inTransform,const Vec2<float>& inSize,int inDepth = 0);

        explicit TransformInfo(const Surface * inSurface);

        explicit TransformInfo(const Shared<Surface>& inSurface);

        explicit  TransformInfo(const Shared<Widget>& widget,bool absolute = false);

        TransformInfo AccountFor(const Shared<ContainerSlot>& slot) const;
        TransformInfo AccountFor(const Shared<Widget>& widget) const;
        TransformInfo AccountFor(const Widget * widget) const;

        bool IsPointWithin(const Vec2<float>& point) const;

        Rect ComputeAxisAlignedBoundingRect() const;
    };
}
