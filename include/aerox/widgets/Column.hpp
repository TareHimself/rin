#pragma once
#include "Scrollable.hpp"
#include "SlotBase.hpp"

namespace aerox::widgets {
class Column : public Scrollable<SlotBase> {
  
public:

  float scrollScale = 3.0f;
  
  std::optional<uint32_t> GetMaxSlots() const override;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  float GetMaxScroll() const override;
  bool IsScrollable() const override;
  
  Size2D ComputeDesiredSize() const override;

  bool OnScroll(const std::shared_ptr<window::ScrollEvent> &event) override;
};
}
