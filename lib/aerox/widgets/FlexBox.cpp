#include "aerox/widgets/FlexBox.hpp"

namespace aerox::widgets {
std::optional<uint32_t> FlexBox::GetMaxSlots() const {
  return {};
}

void FlexBox::Draw(WidgetFrameData *frameData, DrawInfo info) {
  
}

Size2D FlexBox::ComputeDesiredSize() const {
  return {0,0};
}


}
