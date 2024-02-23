#pragma once
#include "WidgetRoot.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/window/types.hpp"

#include <variant>

namespace vengine {
namespace drawing {
struct RawFrameData;
class DrawingSubsystem;
}
}

namespace vengine::widget {
class WidgetSubsystem;

class Widget : public Object<WidgetSubsystem> {
  Widget * _parent = nullptr;
  WidgetRoot * _root = nullptr;
  
  float _initAt = 0.0f;
  Point2D _pivot;
  std::optional<Size2D> _cachedDesiredSize;
  EVisibility _visibility = EVisibility::Visibility_Visible;
  Rect _drawRect{};
  bool _isHovered = false;
  WidgetRoot * _lastRoot = nullptr;
  bool _isOnScreen = false;
protected:
  Array<Managed<Widget>> _children;
public:
  void Init(WidgetSubsystem * outer) override;
  
  // virtual void Init(Widget * parent);

  virtual void CheckDesiredSize();
  virtual void InvalidateCachedSize();
  virtual void SetVisibility(const EVisibility& visibility);
  virtual EVisibility GetVisibility() const;
  void SetPivot(const Point2D& pivot);
  Point2D GetPivot() const;
  virtual void NotifyRootChanged(WidgetRoot * root);
  void SetParent(Widget * ptr);

  void SetLastDrawRect(const Rect& rect);

  Rect GetDrawRect() const;
  
  Widget * GetParentWidget() const;
  WidgetRoot * GetRoot() const;

  float GetTimeAtInit() const;
  
  virtual void Draw(WidgetFrameData *frameData, DrawInfo
                    info);

  void BeforeDestroy() override;

  virtual bool IsOnScreen();

  virtual Size2D GetDesiredSize();

  virtual Rect CalculateFinalRect(const Rect& rect);
  
  bool IsHovered() const;
  
  virtual Size2D ComputeDesiredSize() const;

  virtual bool IsHitTestable() const;
  
  virtual bool ReceiveMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);

  virtual bool OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);
  
  virtual void OnMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event);

  virtual void ReceiveMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event,std::list<Ref<Widget>> &items);

  virtual bool ReceiveMouseMove(
      const std::shared_ptr<window::MouseMovedEvent> &event);

  virtual bool OnMouseMoved(const std::shared_ptr<window::MouseMovedEvent> &event);
  
  virtual void OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event);
  
  virtual void OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event);

  virtual bool ReceiveScroll(const std::shared_ptr<window::ScrollEvent> &event);
  
  virtual bool OnScroll(const std::shared_ptr<window::ScrollEvent>& event);

  virtual Rect UpdateDrawRect(const Rect& rect);

  virtual std::optional<Size2D> GetCachedDesiredSize() const;

  virtual void NotifyAddedToScreen();
  virtual void NotifyRemovedFromScreen();
  virtual void OnAddedToScreen();
  virtual void OnRemovedFromScreen();
};
}

