#pragma once
#include <vector>
#include <optional>
#include "WidgetVisibility.hpp"
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
class ContainerWidget;

    class Widget : public Disposable
    {
        DelegateListHandle _cursorUpBinding{};
        Weak<WidgetSurface> _surface{};
        Weak<ContainerWidget> _parent{};
        Vec2<float> _offset{0.0};
        Vec2<float> _size{0.0};
        std::optional<Vec2<float>> _cachedDesiredSize{};
        bool _hovered = false;
        Padding _padding{0.0};
        
        WidgetVisibility _visibility = WidgetVisibility::Visible;

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

        // Computes the size of the widget's content (i.e. where draws will happen or other widgets will be placed)
        virtual Vec2<float> ComputeContentSize() = 0;

        // Computes the size of the widget (Content size + Padding)
        virtual Vec2<float> ComputeDesiredSize();

        // Collects draw commands
        virtual void CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands) = 0;
    public:

        Vec2<float> pivot{0.0};
        float angle{0.0};
        Vec2<float> scale{1.0};
        
        Shared<WidgetSurface> GetSurface() const;
        Shared<ContainerWidget> GetParent() const;
        Matrix3<float> ComputeRelativeTransform() const;

        Matrix3<float> ComputeAbsoluteTransform() const;

        void SetVisibility(WidgetVisibility visibility);

        WidgetVisibility GetVisibility() const;

        WidgetRect GetRect() const;

        void SetParent(const Shared<ContainerWidget>& container);
        void SetSurface(const Shared<WidgetSurface>& surface);

        bool IsHovered() const;

        bool IsHitTestable() const;
        
        bool IsSelfHitTestable() const;

        bool AreChildrenHitTestable() const;

        bool HasSurface() const;

        bool HasParent() const;
        
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
        
        // Returns the offset of this widget in parent space
        Vec2<float> GetOffset() const;

        // Sets the offset of this widget in parent space (May perform side effects)
        virtual void SetOffset(const Vec2<float>& offset);

        // Returns the size this widget will draw at in parent space
        Vec2<float> GetSize() const;

        Vec2<float> GetContentSize() const;

        // Sets the size of this widget in parent space (May perform side effects)
        virtual void SetSize(const Vec2<float>& size);

        Vec2<float> GetDesiredSize();

        Padding GetPadding() const;
        
        void SetPadding(const Padding& padding);

        std::optional<Vec2<float>> GetCachedDesiredSize() const;

        // Will check if the desired size of this widget has changed
        bool CheckSize();
        
        // Collects draw commands
        virtual void Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands);

        //void Collect(WidgetFrame frame, DrawInfo info);
    };
