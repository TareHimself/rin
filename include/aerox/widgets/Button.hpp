#pragma once
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"
#include "Widget.hpp"

namespace aerox::widgets {
class Button : public TMultiSlotWidget<SlotBase> {
  
public:

  DECLARE_DELEGATE(onPressed,std::shared_ptr<Button>&,const std::shared_ptr<window::MouseButtonEvent>&)
  DECLARE_DELEGATE(onReleased,std::shared_ptr<Button>&,const std::shared_ptr<window::MouseButtonEvent>&)
  std::optional<uint32_t> GetMaxSlots() const override;

  bool OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event) override;
  void OnMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event) override;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Size2D ComputeDesiredSize() const override;
};
}

