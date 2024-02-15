#include "vengine/widget/Sizer.hpp"

namespace vengine::widget {
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
    drawing::SimpleFrameData *frameData, const DrawInfo info) {

  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  
  if(!clip.HasSpace()) {
    return;
  }
  
  if(auto slot = _slots.try_index(0); slot.has_value()) {
    if(auto widget = slot.value()->GetWidget().Reserve()) {
      auto drawRect = widget->UpdateDrawRect(Rect().Offset(GetDrawRect().GetPoint()).SetSize(GetDrawRect().GetSize()));
      widget->Draw(frameData,{this,clip});
    }
  }
}

Size2D Sizer::ComputeDesiredSize() const {
  if(auto slot = _slots.try_index(0); slot.has_value()) {
    auto childSize = slot.value()->GetWidget().Reserve()->GetDesiredSize();
    return {_width.value_or(childSize.width),_height.value_or(childSize.height)};
  }
  
  
  return {};
}

}

