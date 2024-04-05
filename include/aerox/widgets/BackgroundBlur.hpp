#pragma once
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"

namespace aerox::widgets {

class BackgroundBlur : public TMultiSlotWidget<SlotBase>{
public:
  float blurRadius = 5.0f;

  std::optional<uint32_t> GetMaxSlots() const override;
};
}
