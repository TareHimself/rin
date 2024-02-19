#include "vengine/widget/Row.hpp"

std::optional<uint32_t> vengine::widget::Row::GetMaxSlots() const {
  return {};
}

void vengine::widget::Row::Draw(
    vengine::widget::WidgetFrameData *frameData, const DrawInfo info) {
  
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }
  
  auto offset = IsScrollable() ? GetScrollOffset() : 0.0f;
  
  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  
  if(!clip.HasSpace()) {
    return;
  }
  
  for(auto &slot : _slots.clone()) {
    if(auto widget = slot->GetWidget().Reserve()) {
      auto size = widget->GetDesiredSize();
      const auto slotRect = widget->UpdateDrawRect(Rect().SetPoint({offset,0.0f}).Offset(GetDrawRect().GetPoint()).SetSize(size));
      
      widget->Draw(frameData, {this,clip});
      offset += size.width;
    }
  }
}

vengine::widget::Size2D vengine::widget::Row::ComputeDesiredSize() const {
  auto size = Size2D{0.0f,0.0f};

  for(auto &slot : _slots) {
    auto slotSize = slot->GetWidget().Reserve()->GetDesiredSize();
    size.height = std::max(size.height,slotSize.height);
    size.width += slotSize.width;
  }
  return size;
}

float vengine::widget::Row::GetMaxScroll() const {
  return GetCachedDesiredSize().value_or(Size2D()).width - GetDrawRect().GetSize().width;
}

bool vengine::widget::Row::IsScrollable() const {
  return GetMaxScroll() > 0.0f;
}

bool vengine::widget::Row::OnScroll(
    const std::shared_ptr<window::ScrollEvent> &event) {
  
  return ScrollBy(event->dy * scrollScale);
}

bool vengine::widget::Row::OnMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  _lastMousePosition = glm::dvec2{event->x,event->y};
  return true;
}

void vengine::widget::Row::OnMouseUp(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  _lastMousePosition.reset();
  Scrollable<SlotBase>::OnMouseUp(event);
}

bool vengine::widget::Row::OnMouseMoved(const std::shared_ptr<window::MouseMovedEvent> &event) {
  if(_lastMousePosition.has_value()) {
    auto newPosition = glm::dvec2{event->x,event->y};
    const auto delta = _lastMousePosition.value() - newPosition;
    ScrollBy(delta.x);
    _lastMousePosition = newPosition;
    return true;
  }
  return Scrollable<SlotBase>::OnMouseMoved(event);
}
