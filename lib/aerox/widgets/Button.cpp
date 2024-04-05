#include "aerox/widgets/Button.hpp"

namespace aerox::widgets {

std::optional<uint32_t> Button::GetMaxSlots() const {
  return 1;
}

bool Button::OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event) {
  if(onPressed->IsBound() || onReleased->IsBound()) {
    if(onPressed->IsBound()) {
      auto self = utils::cast<Button>(this->shared_from_this());
      onPressed->Execute(self,event);
    }
    return true;
  }
  return TMultiSlotWidget::OnMouseDown(event);
}

void Button::OnMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event) {
  if(onReleased->IsBound()) {
    auto self = utils::cast<Button>(this->shared_from_this());
    onReleased->Execute(self,event);
  }
  TMultiSlotWidget::OnMouseUp(event);
}

void Button::Draw(WidgetFrameData *frameData, DrawInfo info) {
  TMultiSlotWidget::Draw(frameData, info);
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  
  if(!clip.HasSpace()) {
    return;
  }
  
  if(const auto slot = _slots.try_index(0); slot.has_value()) {
    if(const auto widget = slot.value()->GetWidget().lock()) {
      auto drawRect = widget->UpdateDrawRect(Rect().Offset(GetDrawRect().GetPoint()).SetSize(GetDrawRect().GetSize()));
      widget->Draw(frameData,{this,clip});
    }
  }
}

Size2D Button::ComputeDesiredSize() const {
  if(GetNumOccupiedSlots() == 0) {
    return {0,0};
  }

  const auto slot = GetSlots().front().lock();

  return slot->GetWidget().lock()->GetDesiredSize();
}
}
