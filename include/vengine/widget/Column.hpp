#pragma once
#include "Scrollable.hpp"

namespace vengine::widget {
class Column : public Scrollable<SlotBase> {
public:
  std::optional<uint32_t> GetMaxSlots() const override;

  void Draw(drawing::SimpleFrameData *frameData, DrawInfo info) override;

  float GetMaxScroll() const override;
  bool IsScrollable() const override;
  
  Size2D ComputeDesiredSize() const override;

  bool OnScroll(const std::shared_ptr<window::ScrollEvent> &event) override;
};
}
