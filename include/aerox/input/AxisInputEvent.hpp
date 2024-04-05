#pragma once
#include "InputEvent.hpp"
#include "types.hpp"

namespace aerox::input {
class AxisInputEvent : public InputEvent {
  float _value;
  EInputAxis _axis;
public:
  AxisInputEvent(EInputAxis axis,const float &value);
  float GetValue() const;
  std::string GetName() const override;
};
}
