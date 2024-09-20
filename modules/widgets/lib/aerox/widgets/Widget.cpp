#include "aerox/widgets/Widget.hpp"
#include "aerox/widgets/Container.hpp"
#include "aerox/widgets/Surface.hpp"
#include "aerox/widgets/event/CursorDownEvent.hpp"

namespace aerox::widgets
{
    void Widget::OnAddedToSurface(const Shared<Surface>& widgetSurface)
    {
        _surface = widgetSurface;
    }

    void Widget::OnRemovedFromSurface(const Shared<Surface>& widgetSurface)
    {
        _surface = {};
    }

    bool Widget::OnCursorDown(const Shared<CursorDownEvent>& event)
    {
        return false;
    }

    void Widget::OnCursorUp(const Shared<CursorUpEvent>& event)
    {
    }

    bool Widget::OnCursorMove(const Shared<CursorMoveEvent>& event)
    {
        return false;
    }

    void Widget::OnCursorEnter(const Shared<CursorMoveEvent>& event)
    {
    }

    void Widget::OnCursorLeave(const Shared<CursorMoveEvent>& event)
    {
    }

    bool Widget::OnScroll(const Shared<ScrollEvent>& event)
    {
        return false;
    }

    void Widget::OnFocus()
    {
    }

    void Widget::OnFocusLost()
    {
    }

    Vec2<float> Widget::ComputeFinalDesiredSize()
    {
        return ComputeDesiredSize() + Vec2<float>{padding.left + padding.right, +padding.top + padding.bottom};
    }

    Shared<Surface> Widget::GetSurface() const
    {
        if(auto surface = _surface.lock())
        {
            return surface;
        }

        return  {};
    }

    Shared<Container> Widget::GetParent() const
    {
        if(auto parent = _parent.lock())
        {
            return parent;
        }

        return  {}; 
    }

    Matrix3<float> Widget::ComputeRelativeTransform() const
    {
        auto mat = Matrix3<float>(1.0f);
        auto origin = (_relativeOffset + Vec2<float>(padding.left,padding.top));
        // auto offset = 
        // auto pivotDelta = ;
        // auto totalDelta = offset + _relativeOffset + pivotDelta;
        mat = mat.Translate(GetDrawSize() * pivot * -1.0f).RotateDeg(angle).Translate(origin).Scale(scale);
        // mat = mat.Translate(totalDelta);
        // mat = mat.RotateDeg(angle);
        // mat = mat.Scale(scale);
        // mat = mat.Translate(totalDelta * -1.0f);
        return mat;
    }

    Matrix3<float> Widget::ComputeAbsoluteTransform() const
    {
        if(auto parent = GetParent())
        {
            return ComputeRelativeTransform() * parent->ComputeAbsoluteTransform();
        }
        return ComputeRelativeTransform();
    }

    void Widget::SetVisibility(EVisibility visibility)
    {
        _visibility = visibility;
    }

    EVisibility Widget::GetVisibility() const
    {
        return _visibility;
    }

    Rect Widget::GetRect() const
    {
        return Rect{_relativeOffset,_drawSize};
    }
    

    void Widget::SetParent(const Shared<Container>& container)
    {
        _parent = container;
    }

    void Widget::SetSurface(const Shared<Surface>& surface)
    {
        _surface = surface;
    }

    bool Widget::IsHovered() const
    {
        return _hovered;
    }

    bool Widget::IsHitTestable() const
    {
        return IsSelfHitTestable() || AreChildrenHitTestable();
    }

    bool Widget::IsSelfHitTestable() const
    {
        return _visibility == EVisibility::Visible || _visibility == EVisibility::VisibleNoHitTestChildren;
    }

    bool Widget::AreChildrenHitTestable() const
    {
        return _visibility == EVisibility::Visible || _visibility == EVisibility::VisibleNoHitTestSelf;
    }

    void Widget::BindCursorUp(const Shared<Surface>& surface)
    {
        _cursorUpBinding.UnBind();
        _cursorUpBinding = surface->onCursorUp->Add(this->GetSharedDynamic<Widget>(),&Widget::OnCursorUp);
    }

    void Widget::UnBindCursorUp()
    {
        _cursorUpBinding.UnBind();
    }

    void Widget::NotifyAddedToSurface(const Shared<Surface>& widgetSurface)
    {
        _surface = widgetSurface;
        OnAddedToSurface(widgetSurface);
    }

    void Widget::NotifyRemovedFromSurface(const Shared<Surface>& widgetSurface)
    {
        _surface = {};
        OnRemovedFromSurface(widgetSurface);
    }

    Shared<Widget> Widget::NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform)
    {
        if (IsSelfHitTestable() && OnCursorDown(event))
        {
            BindCursorUp(event->surface);
            return this->GetSharedDynamic<Widget>();
        }
        return {};
    }

    void Widget::NotifyCursorUp(const Shared<CursorUpEvent>& event)
    {
        UnBindCursorUp();
        OnCursorUp(event);
    }

    void Widget::NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                   std::vector<Shared<Widget>>& items)
    {
        if(!IsSelfHitTestable()) return;
        items.push_back(this->GetSharedDynamic<Widget>());
        if(IsHovered()) return;

        _hovered = true;
        OnCursorEnter(event);
    }

    bool Widget::NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        if (IsSelfHitTestable() && OnCursorMove(event)) return true;

        return false;
    }

    bool Widget::NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
    {
        return IsSelfHitTestable() && OnScroll(event);
    }

    void Widget::NotifyCursorLeave(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        if (!IsHovered()) return;

        _hovered = false;
        OnCursorLeave(event);
    }

    Vec2<float> Widget::GetRelativeOffset() const
    {
        return _relativeOffset;
    }

    void Widget::SetRelativeOffset(const Vec2<float>& offset)
    {
        _relativeOffset = offset;
    }

    Vec2<float> Widget::GetDrawSize() const
    {
        return _drawSize;
    }

    void Widget::SetDrawSize(const Vec2<float>& size)
    {
        _drawSize = size;
    }

    Vec2<float> Widget::GetDesiredSize()
    {
        return _cachedDesiredSize.has_value() ? _cachedDesiredSize.value() : ComputeFinalDesiredSize();
    }

    bool Widget::CheckSize()
    {
        auto newSize = ComputeFinalDesiredSize();
        if(_cachedDesiredSize.has_value() && _cachedDesiredSize->NearlyEquals(newSize,0.01f)) return false;

        _cachedDesiredSize = newSize;
        if(auto parent = GetParent())
        {
            parent->OnChildResized(this);
        }

        return true;
    }
}
