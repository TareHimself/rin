#include "aerox/widgets/Row.hpp"

namespace aerox::widgets {
std::optional<uint32_t> Row::GetMaxSlots() const {
  return {};
}

void Row::Draw(
    WidgetFrameData *frameData, const DrawInfo info) {
  
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }
  
  auto offset = IsScrollable() ? GetScrollOffset() : 0.0f;
  
  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  
  if(!clip.HasSpace()) {
    return;
  }
  
  for(const auto &slot : _slots.clone()) {
    if(const auto widget = slot->GetWidget().lock()) {
      auto size = widget->GetDesiredSize();
      const auto slotRect = widget->UpdateDrawRect(Rect().SetPoint({offset,0.0f}).Offset(GetDrawRect().GetPoint()).SetSize(size));
      
      widget->Draw(frameData, {this,clip});
      offset += size.width;
    }
  }
}

Size2D Row::ComputeDesiredSize() const {
  auto size = Size2D{0.0f,0.0f};

  for(auto &slot : _slots) {
    auto slotSize = slot->GetWidget().lock()->GetDesiredSize();
    size.height = std::max(size.height,slotSize.height);
    size.width += slotSize.width;
  }
  return size;
}

float Row::GetMaxScroll() const {
  return GetCachedDesiredSize().value_or(Size2D()).width - GetDrawRect().GetSize().width;
}

bool Row::IsScrollable() const {
  return GetMaxScroll() > 0.0f;
}

bool Row::OnScroll(
    const std::shared_ptr<aerox::window::ScrollEvent> &event) {
  
  return ScrollBy(event->dy * scrollScale);
}

bool Row::OnMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  _lastMousePosition = glm::dvec2{event->x,event->y};
  return true;
}

void Row::OnMouseUp(const std::shared_ptr<aerox::window::MouseButtonEvent> &event) {
  _lastMousePosition.reset();
  Scrollable<SlotBase>::OnMouseUp(event);
}

bool Row::OnMouseMoved(const std::shared_ptr<aerox::window::MouseMovedEvent> &event) {
  if(_lastMousePosition.has_value()) {
    auto newPosition = glm::dvec2{event->x,event->y};
    const auto delta = _lastMousePosition.value() - newPosition;
    ScrollBy(delta.x);
    _lastMousePosition = newPosition;
    return true;
  }
  return Scrollable<SlotBase>::OnMouseMoved(event);
}
}
