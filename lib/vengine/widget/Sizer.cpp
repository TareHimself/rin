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
  const auto myDrawRect = CalculateFinalRect(info.drawRect);
  if(auto slot = _slots.try_index(0); slot.has_value()) {
    slot.value()->GetWidget().Reserve()->Draw(frameData, {this,myDrawRect});
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

