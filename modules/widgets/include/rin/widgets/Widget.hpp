#pragma once
#include <vector>
#include <optional>
#include "EVisibility.hpp"
#include "Padding.hpp"
#include "WidgetRect.hpp"
#include "TransformInfo.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/core/memory.hpp"
#include "rin/core/delegates/DelegateList.hpp"
#include "rin/core/math/Matrix3.hpp"
#include "rin/core/math/Vec2.hpp"
#include "graphics/WidgetDrawCommand.hpp"

class WidgetDrawCommands;
class CursorUpEvent;
class ScrollEvent;
class CursorMoveEvent;
class CursorDownEvent;
class WidgetSurface;
class WidgetContainer;

    class Widget : public Disposable
    {
        DelegateListHandle _cursorUpBinding{};
        Weak<WidgetSurface> _surface{};
        Weak<WidgetContainer> _parent{};
        Vec2<float> _relativeOffset{0.0};
        Vec2<float> _drawSize{0.0};
        std::optional<Vec2<float>> _cachedDesiredSize{};
        bool _hovered = false;
        Padding _padding{0.0};
        
        EVisibility _visibility = EVisibility::Visible;

        friend WidgetSurface;
    protected:
        virtual void OnAddedToSurface(const Shared<WidgetSurface>& widgetSurface);

        virtual void OnRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface);
        
        virtual bool OnCursorDown(const Shared<CursorDownEvent>& event);

        virtual void OnCursorUp(const Shared<CursorUpEvent>& event);

        virtual bool OnCursorMove(const Shared<CursorMoveEvent>& event);

        virtual void OnCursorEnter(const Shared<CursorMoveEvent>& event);

        virtual void OnCursorLeave(const Shared<CursorMoveEvent>& event);

        virtual bool OnScroll(const Shared<ScrollEvent>& event);

        virtual void OnFocus();

        virtual void OnFocusLost();

        virtual Vec2<float> ComputeContentSize() = 0;

        virtual Vec2<float> ComputeDesiredSize();
    public:

        Vec2<float> pivot{0.0};
        float angle{0.0};
        Vec2<float> scale{1.0};
        
        Shared<WidgetSurface> GetSurface() const;
        Shared<WidgetContainer> GetParent() const;
        Matrix3<float> ComputeRelativeTransform() const;

        Matrix3<float> ComputeAbsoluteTransform() const;

        void SetVisibility(EVisibility visibility);

        EVisibility GetVisibility() const;

        WidgetRect GetRect() const;

        void SetParent(const Shared<WidgetContainer>& container);
        void SetSurface(const Shared<WidgetSurface>& surface);

        bool IsHovered() const;

        bool IsHitTestable() const;
        
        bool IsSelfHitTestable() const;

        bool AreChildrenHitTestable() const;
        
        void BindCursorUp(const Shared<WidgetSurface>& surface);

        void UnBindCursorUp();

        virtual void NotifyAddedToSurface(const Shared<WidgetSurface>& widgetSurface);

        virtual void NotifyRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface);
        
        virtual Shared<Widget> NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform);

        virtual void NotifyCursorUp(const Shared<CursorUpEvent>& event);

        virtual void NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items);

        virtual bool NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);

        virtual bool NotifyScroll(const Shared<ScrollEvent>& event,const TransformInfo& transform);

        virtual void NotifyCursorLeave(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);

        Vec2<float> GetRelativeOffset() const;

        virtual void SetRelativeOffset(const Vec2<float>& offset);

        Vec2<float> GetDrawSize() const;

        Vec2<float> GetContentSize() const;

        virtual void SetDrawSize(const Vec2<float>& size);

        Vec2<float> GetDesiredSize();

        Padding GetPadding() const;
        void SetPadding(const Padding& padding);

        std::optional<Vec2<float>> GetCachedDesiredSize() const;
        
        bool CheckSize();

        virtual void Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands) = 0;

        //void Collect(WidgetFrame frame, DrawInfo info);
    };
