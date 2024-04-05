#pragma once
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"

namespace aerox::widgets {
class Overlay : public TMultiSlotWidget<>{
public:
  std::optional<uint32_t> GetMaxSlots() const override;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Size2D ComputeDesiredSize() const override;
};
}
