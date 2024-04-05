#include "aerox/widgets/Sizer.hpp"

namespace aerox::widgets {
void Sizer::SetWidth(const std::optional<float> &width) {
  _width = width;
  InvalidateCachedSize();
}

void Sizer::SetHeight(const std::optional<float> &height) {
  _height = height;
  InvalidateCachedSize();
}

std::optional<float> Sizer::GetWidth() const {
  return _width;
}

std::optional<float> Sizer::GetHeight() const {
  return _height;
}

std::optional<uint32_t> Sizer::GetMaxSlots() const {
  return 1;
}

void Sizer::Draw(
    WidgetFrameData *frameData, const DrawInfo info) {

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

Size2D Sizer::ComputeDesiredSize() const {
  if(const auto slot = _slots.try_index(0); slot.has_value()) {
    auto childSize = slot.value()->GetWidget().lock()->GetDesiredSize();
    return {_width.value_or(childSize.width),_height.value_or(childSize.height)};
  }
  
  
  return {};
}

}

