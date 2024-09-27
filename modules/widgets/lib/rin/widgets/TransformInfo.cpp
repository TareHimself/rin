#include "rin/widgets/TransformInfo.hpp"

#include "rin/widgets/WidgetContainerSlot.hpp"
#include "rin/widgets/WidgetSurface.hpp"
#include "rin/widgets/Widget.hpp"

TransformInfo::TransformInfo(const Matrix3<float>& inTransform, const Vec2<float>& inSize,int inDepth)
    {
        transform = inTransform;
        size = inSize;
        depth = inDepth;
    }

    TransformInfo::TransformInfo(const WidgetSurface* inSurface) : TransformInfo(Matrix3<float>{1.0f},inSurface->GetDrawSize().Cast<float>())
    {
        
    }

    TransformInfo::TransformInfo(const Shared<WidgetSurface>& inSurface) : TransformInfo(Matrix3<float>{1.0f},inSurface->GetDrawSize().Cast<float>())
    {
        
    }

    TransformInfo::TransformInfo(const Shared<Widget>& widget, bool absolute) : TransformInfo(absolute ? widget->ComputeAbsoluteTransform() : widget->ComputeRelativeTransform(),widget->GetDrawSize())
    {
        
    }

    TransformInfo TransformInfo::AccountFor(const Shared<WidgetContainerSlot>& slot) const
    {
        return AccountFor(slot->GetWidget());
    }

    TransformInfo TransformInfo::AccountFor(const Shared<Widget>& widget) const
    {
        return AccountFor(widget.get());
    }

    TransformInfo TransformInfo::AccountFor(const Widget* widget) const
    {
        auto newTransform = transform * widget->ComputeRelativeTransform();
        return TransformInfo{newTransform,widget->GetDrawSize(),depth + 1};
    }
    
    bool TransformInfo::IsPointWithin(const Vec2<float>& point) const
    {
        auto tl = Vec2<float>(0.0f);
        auto br =  size;
        auto tr = Vec2<float>(br.x, tl.y);
        auto bl = Vec2<float>(tl.x, br.y);

        tl =  transform * tl;
        br = transform * br;
        tr = transform * tr;
        bl = transform * bl;

        auto p1AABB = Vec2<float>(
            std::min(std::min(tl.x,tr.x),std::min(bl.x,br.x)),
            std::min(std::min(tl.y,tr.y),std::min(bl.y,br.y))
        );
        auto p2AABB = Vec2<float>(
            std::max(std::max(tl.x,tr.x),std::max(bl.x,br.x)),
            std::max(std::max(tl.y,tr.y),std::max(bl.y,br.y))
        );

        WidgetRect aabb(p1AABB,p2AABB - p1AABB);
        
        if(!aabb.Contains(point)) return false;


        auto top = tr - tl;
        auto right = br - tr;
        auto bottom = bl - br;
        auto left = tl - bl;
        auto pTop = point - tl;
        auto pRight = point - tr;
        auto pBottom = point - br;
        auto pLeft = point - bl;
        
        auto a = top.Acos(pTop);
        auto b = right.Cross(pRight);
        auto c = bottom.Cross(pBottom);
        auto d = left.Cross(pLeft);

        if (a >= 0)
        {
            return b >= 0 && c >= 0 && d >= 0;
        }
        else
        {
            return b < 0 && c < 0 && d < 0;
        }
    }


    WidgetRect TransformInfo::ComputeAxisAlignedBoundingRect() const
    {
        auto tl = Vec2<float>(0.0f);
        auto br =  size;
        auto tr = Vec2<float>(br.x, tl.y);
        auto bl = Vec2<float>(tl.x, br.y);

        tl =  transform * tl;
        br = transform * br;
        tr = transform * tr;
        bl = transform * bl;

        auto p1AABB = Vec2<float>(
            std::min(std::min(tl.x,tr.x),std::min(bl.x,br.x)),
            std::min(std::min(tl.y,tr.y),std::min(bl.y,br.y))
        );
        auto p2AABB = Vec2<float>(
            std::max(std::max(tl.x,tr.x),std::max(bl.x,br.x)),
            std::max(std::max(tl.y,tr.y),std::max(bl.y,br.y))
        );

        return {p1AABB,p2AABB - p1AABB};
    }
