#pragma once
#include <optional>

#include "rin/gui/graphics/DrawCommands.h"
#include "Visibility.h"
#include "Padding.h"
#include "TransformInfo.h"
#include "events/CursorButtonEvent.h"
#include "events/CursorDownEvent.h"
#include "events/CursorEnterEvent.h"
#include "events/CursorMoveEvent.h"
#include "events/KeyEvent.h"
#include "events/ScrollEvent.h"
#include "rin/core/Atomic.h"
#include "rin/core/Disposable.h"
#include "rin/core/memory.h"
#include "rin/core/math/Mat3.h"
#include "rin/core/math/Vec2.h"
namespace rin::gui
{
    class Surface;
}
namespace rin::gui
{
    class CompositeWidget;
}
namespace rin::gui
{
    class Widget : public Disposable
    {
        bool _hovered{false};
        float _angle{0};
        Vec2<> _translate;
        Vec2<> _scale;
        Vec2<> _offset;
        Vec2<> _size;
        Vec2<> _pivot;
        Padding _padding{0};
        Visibility _visibility{Visibility::Visible};
        std::optional<Mat3<>> _cachedRelativeTransform{};
        std::optional<Vec2<>> _cachedDesiredSize{};
        CompositeWidget * _parent{nullptr};
        Surface * _surface{nullptr};
    protected:
        virtual Vec2<> ComputeDesiredContentSize() = 0;
        Vec2<> ComputeDesiredSize();
        
        virtual void OnCursorEnter(const Shared<CursorMoveEvent>& event);
        virtual void OnCursorMove(const Shared<CursorMoveEvent>& event);
        virtual void OnCursorLeave(const Shared<CursorMoveEvent>& event);
        virtual bool OnCursorDown(const Shared<CursorButtonEvent>& event);
        virtual void OnCursorUp(const Shared<CursorButtonEvent>& event);
        virtual void OnKey(const Shared<KeyEvent>& event);
        virtual bool OnScroll(const Shared<ScrollEvent>& event);
    public:

        virtual bool NotifyCursorDown(const Shared<CursorDownEvent>& event,const TransformInfo& info);
        virtual void NotifyCursorUp(const Shared<CursorButtonEvent>& event,const TransformInfo& info);
        virtual void NotifyCursorEnter(const Shared<CursorEnterEvent>& event,const TransformInfo& info);
        virtual void NotifyCursorMove(const Shared<CursorMoveEvent>& event,const TransformInfo& info);
        virtual bool NotifyScroll(const Shared<ScrollEvent>& event,const TransformInfo& info);

        CompositeWidget * GetParent() const;
        virtual void SetParent(CompositeWidget * parent);

        Surface * GetSurface() const;
        virtual void SetSurface(Surface * surface);
        
        virtual Vec2<> LayoutContent(const Vec2<>& availableSpace) = 0;
        virtual void Collect(DrawCommands& drawCommands,const TransformInfo& info) = 0;
        
        float GetAngle() const;
        void SetAngle(const float& angle);
        
        Vec2<> GetTranslation() const;
        void SetTranslation(const Vec2<>& translation);

        Vec2<> GetScale() const;
        void SetScale(const Vec2<>& scale);
        
        Vec2<> GetOffset() const;
        void SetOffset(const Vec2<>& offset);
        
        Vec2<> GetSize() const;
        void SetSize(const Vec2<>& size);
        
        Vec2<> GetPivot() const;
        void SetPivot(const Vec2<>& pivot);
        
        Padding GetPadding() const;
        void SetPadding(const Padding& padding);

        Visibility GetVisibility() const;
        void SetVisibility(const Visibility& visibility);

        bool IsVisible() const;
        
        bool IsHitTestable() const;

        Vec2<> ComputeSize(const Vec2<>& availableSpace,const bool& fill = false);

        Vec2<> GetContentSize() const;
        Vec2<> GetDesiredSize();
        Vec2<> GetDesiredContentSize();
    };
}
