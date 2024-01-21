#pragma once
#include "InputEvent.hpp"
#include "types.hpp"

namespace vengine::input {
class AxisInputEvent : public InputEvent {
  float _value;
  EInputAxis _axis;
public:
  AxisInputEvent(EInputAxis axis,const float &value);
  float GetValue() const;
  String GetName() const override;
};
}
