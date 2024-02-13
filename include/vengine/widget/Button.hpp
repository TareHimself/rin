#pragma once
#include "TMultiSlotWidget.hpp"
#include "Widget.hpp"

namespace vengine::widget {
class Button : public TMultiSlotWidget<SlotBase> {
  
public:
  std::optional<uint32_t> GetMaxSlots() const override;
};
}

