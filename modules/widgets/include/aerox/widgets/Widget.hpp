#pragma once
#include <vector>
#include <optional>
#include "EVisibility.hpp"
#include "Padding.hpp"
#include "Rect.hpp"
#include "TransformInfo.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/core/memory.hpp"
#include "aerox/core/delegates/DelegateList.hpp"
#include "aerox/core/math/Matrix3.hpp"
#include "aerox/core/math/Vec2.hpp"

namespace aerox::widgets
{
    class CursorUpEvent;
}

namespace aerox::widgets
{
    class ScrollEvent;
}

namespace aerox::widgets
{
    class CursorMoveEvent;
    class CursorDownEvent;
    class Surface;
}

namespace aerox::widgets
{
    class Container;

    class Widget : public Disposable
    {
        DelegateListHandle _cursorUpBinding{};
        Weak<Surface> _surface{};
        Weak<Container> _parent{};
        Vec2<float> _relativeOffset{0.0};
        Vec2<float> _drawSize{0.0};
        std::optional<Vec2<float>> _cachedDesiredSize{};
        bool _hovered = false;
        
        EVisibility _visibility = EVisibility::Visible;

        friend Surface;
    protected:
        virtual void OnAddedToSurface(const Shared<Surface>& widgetSurface);

        virtual void OnRemovedFromSurface(const Shared<Surface>& widgetSurface);
        
        virtual bool OnCursorDown(const Shared<CursorDownEvent>& event);

        virtual void OnCursorUp(const Shared<CursorUpEvent>& event);

        virtual bool OnCursorMove(const Shared<CursorMoveEvent>& event);

        virtual void OnCursorEnter(const Shared<CursorMoveEvent>& event);

        virtual void OnCursorLeave(const Shared<CursorMoveEvent>& event);

        virtual bool OnScroll(const Shared<ScrollEvent>& event);

        virtual void OnFocus();

        virtual void OnFocusLost();

        virtual Vec2<float> ComputeDesiredSize() = 0;

        virtual Vec2<float> ComputeFinalDesiredSize();
    public:

        Vec2<float> pivot{0.0};
        float angle{0.0};
        Vec2<float> scale{1.0};
        Padding padding{0.0};
        
        Shared<Surface> GetSurface() const;
        Shared<Container> GetParent() const;
        Matrix3<float> ComputeRelativeTransform() const;

        Matrix3<float> ComputeAbsoluteTransform() const;

        void SetVisibility(EVisibility visibility);

        EVisibility GetVisibility() const;

        Rect GetRect() const;

        void SetParent(const Shared<Container>& container);
        void SetSurface(const Shared<Surface>& surface);

        bool IsHovered() const;

        bool IsHitTestable() const;
        
        bool IsSelfHitTestable() const;

        bool AreChildrenHitTestable() const;
        
        void BindCursorUp(const Shared<Surface>& surface);

        void UnBindCursorUp();

        virtual void NotifyAddedToSurface(const Shared<Surface>& widgetSurface);

        virtual void NotifyRemovedFromSurface(const Shared<Surface>& widgetSurface);
        
        virtual Shared<Widget> NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform);

        virtual void NotifyCursorUp(const Shared<CursorUpEvent>& event);

        virtual void NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items);

        virtual bool NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);

        virtual bool NotifyScroll(const Shared<ScrollEvent>& event,const TransformInfo& transform);

        virtual void NotifyCursorLeave(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);

        Vec2<float> GetOffset() const;

        virtual void SetRelativeOffset(const Vec2<float>& offset);

        Vec2<float> GetDrawSize() const;

        virtual void SetDrawSize(const Vec2<float>& size);

        Vec2<float> GetDesiredSize();
        
        bool CheckSize();

        //void Collect(WidgetFrame frame, DrawInfo info);
    };
}
