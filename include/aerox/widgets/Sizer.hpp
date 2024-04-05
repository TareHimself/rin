#pragma once
#include "ISlot.hpp"
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"
#include "aerox/widgets/Widget.hpp"

namespace aerox::widgets {

class Sizer : public TMultiSlotWidget<SlotBase>{
  std::optional<float> _width;
  std::optional<float> _height;
public:
  void SetWidth(const std::optional<float>& width);
  void SetHeight(const std::optional<float>& height);

  std::optional<float> GetWidth() const;
  std::optional<float> GetHeight() const;

  std::optional<uint32_t> GetMaxSlots() const override;
  
  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Size2D ComputeDesiredSize() const override;
};
}
