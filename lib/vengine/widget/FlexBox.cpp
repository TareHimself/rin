#include "vengine/widget/FlexBox.hpp"

namespace vengine::widget {
void FlexBox::Draw(drawing::SimpleFrameData *frameData, DrawInfo info) {
  auto drawRect = CalculateFinalRect(info.drawRect);
}

Size2D FlexBox::ComputeDesiredSize() const {
  return {0,0};
}


}
