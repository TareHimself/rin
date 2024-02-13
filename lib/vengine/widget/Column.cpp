#include "vengine/widget/Column.hpp"

namespace vengine::widget {
std::optional<uint32_t> Column::GetMaxSlots() const {
  return {};
}

void Column::Draw(
    drawing::SimpleFrameData *frameData, const DrawInfo info) {
  const auto rect = CalculateFinalRect(info.drawRect);
  auto offset = 0.0f;
  for(auto &slot : _slots.clone()) {
    auto widget = slot->GetWidget().Reserve();
    auto size = widget->GetDesiredSize();
    const auto slotRect = Rect{{0.0f,offset},{size.width,size.height}}.OffsetBy(rect);
    widget->Draw(frameData, {this,slotRect});
    offset += size.height;
  }
}

Size2D Column::ComputeDesiredSize() const {
  auto size = Size2D{0.0f,0.0f};

  for(auto &slot : _slots) {
    auto slotSize = slot->GetWidget().Reserve()->GetDesiredSize();
    size.height += slotSize.height;
    size.width = std::max(size.width,slotSize.width);
  }

  return size;
}
}
