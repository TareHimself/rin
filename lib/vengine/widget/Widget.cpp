#include <vengine/widget/Widget.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include "vengine/Engine.hpp"
#include "vengine/math/utils.hpp"

namespace vengine::widget {

void Widget::Init(WidgetSubsystem *outer) {
  Object::Init(outer);
  _initAt = Engine::Get()->GetEngineTimeSeconds();
}

void Widget::CheckDesiredSize() {
  
  const auto mySize = ComputeDesiredSize();
  if(mySize != _cachedDesiredSize) {
    _cachedDesiredSize = mySize;
    if(const auto parent = GetParentWidget()) {
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

void Widget::SetParent(
    const std::variant<WidgetRoot *, Widget *, std::nullptr_t> &ptr) {
  _parent = ptr;
  
  if(std::holds_alternative<WidgetRoot *>(ptr)) {
    _lastRoot = std::get<WidgetRoot *>(ptr);
  } else if(std::holds_alternative<Widget *>(ptr)) {
    _lastRoot = std::get<Widget *>(ptr)->GetRoot();
  }
}

void Widget::SetLastDrawRect(const Rect &rect) {
  _drawRect = rect;
}

Rect Widget::GetDrawRect() const {
  return _drawRect;
}

Widget * Widget::GetParentWidget() const {
  if(std::holds_alternative<Widget *>(_parent)) {
    return std::get<Widget *>(_parent);
  }
  return nullptr;
}

WidgetRoot * Widget::GetRoot() const {
  if(std::holds_alternative<WidgetRoot *>(_parent)) {
    return std::get<WidgetRoot *>(_parent);
  }

  if(_lastRoot) {
    return _lastRoot;
  }
  
  if(const auto parent = GetParentWidget()) {
    return parent->GetRoot();
  }
  return nullptr;
}

float Widget::GetTimeAtInit() const {
  return _initAt;
}

void Widget::Draw(WidgetFrameData *frameData,
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
  Object::BeforeDestroy();
}

Size2D Widget::GetDesiredSize() {
  if(auto parent = GetParentWidget()) {
    if(!_cachedDesiredSize.has_value()) {
      _cachedDesiredSize = ComputeDesiredSize();
    }

    return _cachedDesiredSize.value();
  }

  if(const auto root = GetRoot()) {
    const auto drawSize = root->GetDrawSize();
    return drawSize;
  }

  return {};
}

Rect Widget::CalculateFinalRect(const Rect &rect) {
  _drawRect = rect.Clone();
  return _drawRect;
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
    if(child->GetDrawRect().IsWithin(point) && child->ReceiveMouseDown(event)) {
      return true;
    }
  }

  if(OnMouseDown(event)) {
    uint64_t unbindMouseUp;
    uint64_t unbindDestroy = onDestroyed.Bind([this,&unbindMouseUp] {
      auto window = GetOuter()->GetEngine()->GetMainWindow().Reserve();
      window->onMouseUp.UnBind(unbindMouseUp);
    });
    
    unbindMouseUp = GetOuter()->GetEngine()->GetMainWindow().Reserve()->onMouseUp.Bind([this,&unbindDestroy,&unbindMouseUp](const std::shared_ptr<window::MouseButtonEvent>& e) {
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
    if(child->GetDrawRect().IsWithin(point)) {
      items.push_front(child);
      child->ReceiveMouseEnter(event,items);
    }
  }

  if(!IsHovered()) {
    OnMouseEnter(event);
  }
}


bool Widget::ReceiveMouseMove(
    const std::shared_ptr<window::MouseMovedEvent> &event) {
  if(!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x,event->y};

  for(const auto &child : _children) {
    if(child->GetDrawRect().IsWithin(point) && child->ReceiveMouseMove(event)) {
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

bool Widget::ReceiveScroll(const std::shared_ptr<window::ScrollEvent> &event) {
  if(!IsHitTestable()) {
    return false;
  }

  const auto point = Point2D{event->x,event->y};

  for(const auto &child : _children) {
    if(child->GetDrawRect().IsWithin(point) && child->ReceiveScroll(event)) {
      return true;
    }
  }
  
  return OnScroll(event);
}

bool Widget::OnScroll(const std::shared_ptr<window::ScrollEvent> &event) {
  return false;
}


Rect Widget::UpdateDrawRect(const Rect &rect) {
  auto myRect = rect.Clone();
  myRect.Pivot(GetPivot());
  _drawRect = myRect;
  return _drawRect;
}

std::optional<Size2D> Widget::GetCachedDesiredSize() const {
  return _cachedDesiredSize;
}


}
