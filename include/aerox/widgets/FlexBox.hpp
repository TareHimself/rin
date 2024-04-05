#pragma once
#include "SlotBase.hpp"
#include "aerox/widgets/TMultiSlotWidget.hpp"

namespace aerox::widgets {
class FlexBox : public TMultiSlotWidget<SlotBase>{
public:
  std::optional<uint32_t> GetMaxSlots() const override;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Size2D ComputeDesiredSize() const override;
};
}
