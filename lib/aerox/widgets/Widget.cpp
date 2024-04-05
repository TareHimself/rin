#include <aerox/Engine.hpp>
#include <aerox/widgets/Widget.hpp>
#include <aerox/widgets/WidgetSubsystem.hpp>

namespace aerox::widgets {

void Widget::OnInit(WidgetSubsystem *owner) {
  TOwnedBy<WidgetSubsystem>::OnInit(owner);
  _initAt = Engine::Get()->GetEngineTimeSeconds();
}

void Widget::CheckDesiredSize() {

  const auto mySize = ComputeDesiredSize();
  if (mySize != _cachedDesiredSize) {
    _cachedDesiredSize = mySize;
    if (const auto parent = GetParentWidget().lock()) {
      parent->CheckDesiredSize();
    }
  }
}

void Widget::InvalidateCachedSize() {
  _cachedDesiredSize.reset();
  CheckDesiredSize();
}

void Widget::SetVisibility(const EVisibility &visibility) {
  _visibility = visibility;
}

EVisibility Widget::GetVisibility() const {
  return _visibility;
}

void Widget::SetPivot(const Point2D &pivot) {
  _pivot = pivot;
}

Point2D Widget::GetPivot() const {
  return _pivot;
}

void Widget::NotifyRootChanged(const std::weak_ptr<WidgetRoot> &root) {
  _root = root;
}


void Widget::SetParent(const std::weak_ptr<Widget> &ptr) {
  _parent = ptr;
}

Rect Widget::GetDrawRect() const {
  return _drawRect;
}

std::weak_ptr<Widget> Widget::GetParentWidget() const {
  return _parent;
}

std::weak_ptr<WidgetRoot> Widget::GetRoot() const {
  return _root;
}

float Widget::GetTimeAtInit() const {
  return _initAt;
}

void Widget::Draw(WidgetFrameData *frameData,
                  DrawInfo info) {
}

void Widget::OnDestroy() {
  Object::OnDestroy();
  if (_pendingMouseUp) {
    _pendingMouseUp->UnBind();
  }
}

bool Widget::IsOnScreen() {
  return _isOnScreen;
}

Size2D Widget::GetDesiredSize() {
  if (GetParentWidget().lock()) {
    if (!_cachedDesiredSize.has_value()) {
      _cachedDesiredSize = ComputeDesiredSize();
    }

    return _cachedDesiredSize.value();
  }

  if (const auto root = GetRoot().lock()) {
    const auto drawSize = root->GetDrawSize();
    return drawSize;
  }

  return {};
}

bool Widget::IsHovered() const {
  return _isHovered;
}

Size2D Widget::ComputeDesiredSize() const {
  return {};
}

bool Widget::IsHitTestable() const {
  return GetVisibility() == EVisibility::Visibility_Visible;
}


bool Widget::ReceiveMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  if (!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x, event->y};

  for (const auto &child : _children) {
    if (child->GetDrawRect().IsWithin(point) && child->
        ReceiveMouseDown(event)) {
      return true;
    }
  }

  if (OnMouseDown(event)) {

    std::weak_ptr<Widget> thisRef = utils::cast<Widget>(this->shared_from_this());

    _pendingMouseUp = GetOwner()->GetOwner()->GetMainWindow().
                                     lock()->onMouseUp->BindFunction(
                                         [thisRef](
                                         const std::shared_ptr<window::MouseButtonEvent>
                                         &e) {
                                           if (const auto managed = thisRef.lock()) {
                                             managed->OnMouseUp(e);
                                           }
                                         }, 1);

    return true;
  }
  return false;
}

bool Widget::OnMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  return false;
}

void Widget::OnMouseUp(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  return;
}

void Widget::ReceiveMouseEnter(
    const std::shared_ptr<window::MouseMovedEvent> &event,
    std::list<std::weak_ptr<Widget>> &items) {
  if (!IsHitTestable()) {
    return;
  }

  const auto point = Point2D{event->x, event->y};

  for (const auto &child : _children) {
    if (child->GetDrawRect().IsWithin(point)) {
      items.push_front(child);
      child->ReceiveMouseEnter(event, items);
    }
  }

  if (!IsHovered()) {
    OnMouseEnter(event);
  }
}


bool Widget::ReceiveMouseMove(
    const std::shared_ptr<window::MouseMovedEvent> &event) {
  if (!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x, event->y};

  for (const auto &child : _children) {
    if (child->GetDrawRect().IsWithin(point) && child->
        ReceiveMouseMove(event)) {
      return true;
    }
  }

  return OnMouseMoved(event);
}

bool Widget::OnMouseMoved(const std::shared_ptr<window::MouseMovedEvent> &event) {
  return false;
}

void Widget::OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event) {
  _isHovered = true;
}

void Widget::OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event) {
  _isHovered = false;
}

void Widget::OnResized(const Size2D &newSize) {
  onResize->Execute(newSize);
}

bool Widget::ReceiveScroll(const std::shared_ptr<window::ScrollEvent> &event) {
  if (!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x, event->y};

  for (const auto &child : _children) {
    if (child->GetDrawRect().IsWithin(point) && child->ReceiveScroll(event)) {
      return true;
    }
  }

  return OnScroll(event);
}

bool Widget::OnScroll(const std::shared_ptr<window::ScrollEvent> &event) {
  return false;
}

void Widget::Tick(float deltaTime) {
  
}


Rect Widget::UpdateDrawRect(const Rect &rect) {
  auto myRect = rect.Clone();
  myRect.Pivot(GetPivot());
  const auto sizeChanged = _drawRect.GetSize() != myRect.GetSize();
  _drawRect = myRect;

  if (sizeChanged) {
    OnResized(_drawRect.GetSize());
  }

  return _drawRect;
}

std::optional<Size2D> Widget::GetCachedDesiredSize() const {
  return _cachedDesiredSize;
}

void Widget::NotifyAddedToScreen() {
  _isOnScreen = true;
  OnAddedToScreen();
}

void Widget::NotifyRemovedFromScreen() {
  _isOnScreen = false;
  OnRemovedFromScreen();
  NotifyRootChanged({});
}

void Widget::OnAddedToScreen() {

}

void Widget::OnRemovedFromScreen() {

}


}
