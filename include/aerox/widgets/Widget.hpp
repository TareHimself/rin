#pragma once
#include "WidgetRoot.hpp"
#include "types.hpp"
#include "aerox/Object.hpp"
#include "aerox/containers/Array.hpp"
#include "aerox/window/types.hpp"

namespace aerox::drawing {
struct RawFrameData;
class DrawingSubsystem;
}

namespace aerox::widgets {
class WidgetSubsystem;

class Widget : public TOwnedBy<WidgetSubsystem> {
  std::weak_ptr<Widget> _parent;
  std::weak_ptr<WidgetRoot> _root;

  float _initAt = 0.0f;
  Point2D _pivot;
  std::optional<Size2D> _cachedDesiredSize;
  EVisibility _visibility = EVisibility::Visibility_Visible;
  Rect _drawRect{};
  bool _isHovered = false;
  bool _isOnScreen = false;
  
protected:
  Array<std::shared_ptr<Widget>> _children;
  DECLARE_DELEGATE_HANDLE(_pendingMouseUp,const std::shared_ptr<window::MouseButtonEvent>&)

public:
  DECLARE_DELEGATE(onResize,Size2D)
  // virtual void OnInit(Widget * parent);
  void OnInit(WidgetSubsystem* owner) override;
  virtual void CheckDesiredSize();
  virtual void InvalidateCachedSize();
  virtual void SetVisibility(const EVisibility& visibility);
  virtual EVisibility GetVisibility() const;
  
  void SetPivot(const Point2D& pivot);
  Point2D GetPivot() const;
  
  void SetParent(const std::weak_ptr<Widget> &ptr);

  Rect GetDrawRect() const;

  std::weak_ptr<Widget> GetParentWidget() const;
  std::weak_ptr<WidgetRoot> GetRoot() const;

  float GetTimeAtInit() const;
  
  virtual void Draw(WidgetFrameData *frameData, DrawInfo
                    info);

  void OnDestroy() override;

  virtual bool IsOnScreen();

  virtual Size2D GetDesiredSize();
  
  bool IsHovered() const;
  
  virtual Size2D ComputeDesiredSize() const;

  virtual bool IsHitTestable() const;

  virtual std::optional<Size2D> GetCachedDesiredSize() const;

  virtual Rect UpdateDrawRect(const Rect& rect);

  
  virtual void NotifyRootChanged(const std::weak_ptr<WidgetRoot>& root);
  
  
  virtual void NotifyAddedToScreen();
  virtual void NotifyRemovedFromScreen();
  virtual void OnAddedToScreen();
  virtual void OnRemovedFromScreen();
  
  virtual bool ReceiveMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);
  
  virtual void ReceiveMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event, std::list<std::weak_ptr<Widget>> &items);

  virtual bool ReceiveMouseMove(
      const std::shared_ptr<window::MouseMovedEvent> &event);

  virtual bool ReceiveScroll(const std::shared_ptr<window::ScrollEvent> &event);

  virtual bool OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);
  
  virtual void OnMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event);

  virtual bool OnMouseMoved(const std::shared_ptr<window::MouseMovedEvent> &event);
  
  virtual void OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event);
  
  virtual void OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event);

  virtual void OnResized(const Size2D& newSize);
  
  virtual bool OnScroll(const std::shared_ptr<window::ScrollEvent> &event);

  virtual void Tick(float deltaTime);

};
}

