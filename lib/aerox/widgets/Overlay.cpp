#include "aerox/widgets/Overlay.hpp"

namespace aerox::widgets {
std::optional<uint32_t> Overlay::GetMaxSlots() const {
  return {};
}

void Overlay::Draw(WidgetFrameData *frameData, DrawInfo info) {
  TMultiSlotWidget::Draw(frameData, info);
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  
  if(!clip.HasSpace()) {
    return;
  }

  for(const auto slot : _slots.clone()) {
    if(const auto widget = slot->GetWidget().lock()) {
      auto drawRect = widget->UpdateDrawRect(Rect().Offset(GetDrawRect().GetPoint()).SetSize(GetDrawRect().GetSize()));
      widget->Draw(frameData,{this,clip});
    }
  }
}

Size2D Overlay::ComputeDesiredSize() const {
  Size2D result = {0,0};
  for(const auto slot : _slots.clone()) {
    if(const auto widget = slot->GetWidget().lock()) {
      auto mySize = widget->GetDesiredSize();
      result.height = std::max(mySize.height,result.height);
      result.width = std::max(mySize.width,result.width);
    }
  }

  return result;
}
}
