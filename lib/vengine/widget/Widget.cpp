#include <vengine/widget/Widget.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include "vengine/Engine.hpp"
#include "vengine/math/utils.hpp"

namespace vengine::widget {
void Widget::Init(WidgetSubsystem * outer) {
  Object<WidgetSubsystem>::Init(outer);
  _initAt = GetOuter()->GetOuter()->GetEngineTimeSeconds();
}

void Widget::CheckDesiredSize() {
  
  const auto mySize = ComputeDesiredSize();
  if(mySize != _cachedDesiredSize) {
    _cachedDesiredSize = mySize;
    if(_parent) {
      _parent->CheckDesiredSize();
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

void Widget::SetParent(Widget *ptr) {
  _parent = ptr;
}

void Widget::SetLastDrawRect(const Rect &rect) {
  _lastDrawRect = rect;
}

Rect Widget::GetLastDrawRect() const {
  return _lastDrawRect;
}

Widget * Widget::GetParent() const {
  return _parent;
}

float Widget::GetTimeAtInit() const {
  return _initAt;
}

void Widget::Draw(drawing::SimpleFrameData *frameData,
                  DrawInfo info) {
}
//
// void Widget::DrawSelf(drawing::Drawer * drawer, drawing::SimpleFrameData *frameData,
//     DrawInfo parentInfo) {
//   // const auto manager = GetOuter();
//   // const auto engine = manager->GetEngine();
//   // const auto material = manager->GetDefaultMaterial().Reserve();
//   // const auto ogFrameData = frameData->GetRaw();
//   // const auto screenSize = engine->GetWindowExtent();
//   // WidgetPushConstants drawData{};
//   // const auto curTime =  engine->GetEngineTimeSeconds() - _createdAt;
//   // const auto sinModified = (sin(curTime + cos(_createdAt))  + 1) / 2;
//   // const auto sinModified2 = (sin(curTime + sinModified * 2)  + 1) / 2;
//   // uint32_t width = math::mapRange(sinModified2, 0, 1, 256, 512);
//   // uint32_t height = math::mapRange(sinModified, 0, 1, 256, 512);
//   // SetRect({{static_cast<int32_t>(sinModified * (screenSize.width - width)),static_cast<int32_t>(sinModified2 * (screenSize.height - height))},{width,height}});
//   //
//   // const auto rect = GetRect();
//   // drawData.extent = glm::vec4{rect.offset.x,rect.offset.y,rect.extent.width,rect.extent.height};
//   // drawData.time.x = curTime;
//   //
//   // material->BindPipeline(ogFrameData);
//   // material->BindSets(ogFrameData);
//   // //material->BindCustomSet(ogFrameData,frameData->GetDescriptor(),0);
//   // material->PushConstant(frameData->GetCmd(),"pRect",drawData);
//   //
//   // frameData->GetCmd()->draw(6,1,0,0);
// }

void Widget::BeforeDestroy() {
  Object<WidgetSubsystem>::BeforeDestroy();
}

Size2D Widget::GetDesiredSize() {
  if(_parent) {
    if(!_cachedDesiredSize.has_value()) {
      _cachedDesiredSize = ComputeDesiredSize();
    }

    return _cachedDesiredSize.value();
  }

  if(const auto outer = GetOuter()) {
    const auto drawSize = outer->GetDrawSize();
    return drawSize;
  }

  return {};
}

Rect Widget::CalculateFinalRect(const Rect &fromParent) {
  _lastDrawRect = fromParent.ApplyPivot(GetPivot());
  return _lastDrawRect;
}

bool Widget::IsInBounds(const Point2D &point) const {
  const auto isWithinHorizontal = _lastDrawRect.x <= point.x && point.x <= _lastDrawRect.x + _lastDrawRect.width;
  const auto isWithinVertical = _lastDrawRect.y <= point.y && point.y <= _lastDrawRect.y + _lastDrawRect.height;
  return isWithinHorizontal && isWithinVertical;
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
  if(!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x,event->y};

  for(const auto &child : _children) {
    if(child->IsInBounds(point) && child->ReceiveMouseDown(event)) {
      return true;
    }
  }

  if(OnMouseDown(event)) {
    uint64_t unbindMouseUp;
    uint64_t unbindDestroy = onDestroyed.Bind([this,&unbindMouseUp] {
      auto window = GetOuter()->GetEngine()->GetWindow().Reserve();
      window->onMouseUp.UnBind(unbindMouseUp);
    });
    
    unbindMouseUp = GetOuter()->GetEngine()->GetWindow().Reserve()->onMouseUp.Bind([this,&unbindDestroy,&unbindMouseUp](const std::shared_ptr<window::MouseButtonEvent>& e) {
      onDestroyed.UnBind(unbindDestroy);
      OnMouseUp(e);
    });

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
    std::list<Ref<Widget>> &items) {
  if(!IsHitTestable()) {
    return;
  }

  
  const auto point = Point2D{event->x,event->y};

  for(const auto &child : _children) {
    if(child->IsInBounds(point)) {
      items.push_front(child);
      child->ReceiveMouseEnter(event,items);
    }
  }

  if(!IsHovered()) {
    OnMouseEnter(event);
  }
}


void Widget::ReceiveMouseMove(const std::shared_ptr<window::MouseMovedEvent> &event) {
  
}

void Widget::OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event) {
  _isHovered = true;
}

void Widget::OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event) {
  _isHovered = false;
}


}
