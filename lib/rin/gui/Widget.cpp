#include "rin/gui/Widget.h"
namespace rin::gui
{
    Vec2<> Widget::ComputeDesiredSize()
    {
        return ComputeDesiredContentSize() + static_cast<Vec2<>>(GetPadding());
    }
    
    void Widget::OnCursorEnter(const Shared<CursorMoveEvent>& event)
    {
    }
    void Widget::OnCursorMove(const Shared<CursorMoveEvent>& event)
    {
    }
    void Widget::OnCursorLeave(const Shared<CursorMoveEvent>& event)
    {
    }
    bool Widget::OnCursorDown(const Shared<CursorButtonEvent>& event)
    {
        return false;
    }
    void Widget::OnCursorUp(const Shared<CursorButtonEvent>& event)
    {
    }
    void Widget::OnKey(const Shared<KeyEvent>& event)
    {
    }
    bool Widget::OnScroll(const Shared<ScrollEvent>& event)
    {
        return false;
    }
    bool Widget::NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& info)
    {
        return OnCursorDown(event);
    }
    void Widget::NotifyCursorUp(const Shared<CursorButtonEvent>& event, const TransformInfo& info)
    {
        OnCursorUp(event);
    }
    void Widget::NotifyCursorEnter(const Shared<CursorEnterEvent>& event, const TransformInfo& info)
    {
        OnCursorEnter(event);
    }
    void Widget::NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& info)
    {
        OnCursorMove(event);
    }
    bool Widget::NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& info)
    {
        return OnScroll(event);
    }
    CompositeWidget* Widget::GetParent() const
    {
        return _parent;
    }
    void Widget::SetParent(CompositeWidget* parent)
    {
        _parent = parent;
    }
    Surface* Widget::GetSurface() const
    {
        return _surface;
    }
    void Widget::SetSurface(Surface* surface)
    {
        _surface = surface;
    }

    float Widget::GetAngle() const
    {
        return _angle;
    }
    void Widget::SetAngle(const float& angle)
    {
        _angle = angle;
    }
    Vec2<> Widget::GetTranslation() const
    {
        return _translate;
    }
    void Widget::SetTranslation(const Vec2<>& translation)
    {
        _translate = translation;
    }
    Vec2<> Widget::GetScale() const
    {
    return _scale;
    }
    void Widget::SetScale(const Vec2<>& scale)
    {
        _scale = scale;
    }
    Vec2<> Widget::GetOffset() const
    {
        return _offset;
    }
    void Widget::SetOffset(const Vec2<>& offset)
    {
        _offset = offset;
    }
    Vec2<> Widget::GetSize() const
    {
        return _size;
    }
    void Widget::SetSize(const Vec2<>& size)
    {
        _size = size;
    }
    Vec2<> Widget::GetPivot() const
    {
        return _pivot;
    }
    void Widget::SetPivot(const Vec2<>& pivot)
    {
        _pivot = pivot;
    }
    Padding Widget::GetPadding() const
    {
        return _padding;
    }
    void Widget::SetPadding(const Padding& padding)
    {
        _padding = padding;
    }
    Visibility Widget::GetVisibility() const
    {
        return _visibility;
    }
    void Widget::SetVisibility(const Visibility& visibility)
    {
        _visibility = visibility;
    }
    bool Widget::IsVisible() const
    {
        const auto visibility = GetVisibility();
        return visibility == Visibility::Visible || visibility == Visibility::VisibleNoHitTestSelf;
    }
    bool Widget::IsHitTestable() const
    {
        const auto visibility = GetVisibility();
       return visibility != Visibility::Hidden && visibility != Visibility::Collapsed;
    }
    Vec2<> Widget::ComputeSize(const Vec2<>& availableSpace, const bool& fill)
    {
        const auto padding = static_cast<Vec2<>>(GetPadding());
        const auto contentSize = LayoutContent(availableSpace - padding) + padding;
        auto sizeResult = fill ? availableSpace : contentSize;
        sizeResult.x = std::isfinite(sizeResult.x) ? sizeResult.x : 0;
        sizeResult.y = std::isfinite(sizeResult.y) ? sizeResult.y : 0;
        SetSize(sizeResult);
        return sizeResult;
    }
    Vec2<> Widget::GetContentSize() const
    {
        return GetSize() - static_cast<Vec2<>>(GetPadding());
    }
    Vec2<> Widget::GetDesiredSize()
    {
        if(auto surface = _surface)
        {
            if(const auto cached  = static_cast<std::optional<Vec2<>>>(_cachedDesiredSize); cached.has_value())
            {
                return cached.value();
            }

            auto size = ComputeDesiredSize();
            _cachedDesiredSize = size;
            return size;
        }

        return ComputeDesiredSize();
    }
    
    Vec2<> Widget::GetDesiredContentSize()
    {
        return GetDesiredSize() - static_cast<Vec2<>>(GetPadding());
    }
}
