#include "vengine/widget/Row.hpp"

std::optional<uint32_t> vengine::widget::Row::GetMaxSlots() const {
  return {};
}

void vengine::widget::Row::Draw(
    drawing::SimpleFrameData *frameData, const DrawInfo info) {
  const auto rect = CalculateFinalRect(info.drawRect);
  auto offset = 0.0f;
  for(auto &slot : _slots.clone()) {
    auto widget = slot->GetWidget().Reserve();
    
    auto size = widget->GetDesiredSize();
    const auto slotRect = Rect{{offset,0.0f},{size.width,size.height}}.OffsetBy(rect);
    widget->Draw(frameData, {this,slotRect});
    offset += size.width;
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
