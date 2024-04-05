#include "aerox/widgets/Column.hpp"

namespace aerox::widgets {
std::optional<uint32_t> Column::GetMaxSlots() const {
  return {};
}

void Column::Draw(
    WidgetFrameData *frameData, const DrawInfo info) {

  
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }
  
  auto offset = IsScrollable() ? GetScrollOffset() : 0.0f;

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());

  if(!clip.HasSpace()) {
    return;
  }

  const auto slots = _slots;
  for(auto &slot : slots) {
    if(const auto widget = slot->GetWidget().lock()) {
      auto size = widget->GetDesiredSize();
      const auto slotRect = widget->UpdateDrawRect(Rect().SetPoint({0.0f,offset}).Offset(GetDrawRect().GetPoint()).SetSize(size));

      // if(slotRect.WillBeClippedBy(rect)) {
      //   break;
      // }
    
      widget->Draw(frameData, {this,clip});
      offset += size.height;
    }
  }
}

float Column::GetMaxScroll() const {
  return GetCachedDesiredSize().value_or(Size2D()).height - GetDrawRect().GetSize().height;
}

bool Column::IsScrollable() const {
  return GetMaxScroll() > 0.0f;
}

Size2D Column::ComputeDesiredSize() const {
  auto size = Size2D{0.0f,0.0f};

  for(auto &slot : _slots) {
    auto slotSize = slot->GetWidget().lock()->GetDesiredSize();
    size.height += slotSize.height;
    size.width = std::max(size.width,slotSize.width);
  }

  return size;
}

bool Column::OnScroll(const std::shared_ptr<window::ScrollEvent> &event) {
  return ScrollBy(event->dy * scrollScale);
}
}
