#pragma once
#include "vengine/widget/TMultiSlotWidget.hpp"

namespace vengine::widget {
class FlexBox : public TMultiSlotWidget<SlotBase>{
public:
  std::optional<uint32_t> GetMaxSlots() const override;

  void Draw(drawing::SimpleFrameData *frameData, DrawInfo info) override;

  Size2D ComputeDesiredSize() const override;
};
}
