#pragma once
#include <ratio>
#include "types.hpp"

namespace aerox::widgets {
// inline std::optional<float> parseUnit(const std::string &val, float axis) {
//   const auto unit = val.at(val.length() - 1);
//   auto size = val.size() - 1;
//   switch (unit) {
//   case 'u': {
//     return std::stof(val, &size);
//   }
//   case '%': {
//     return std::stof(val, &size) * axis;
//   }
//   default:
//     return {};
//   }
// }

struct Unit {
  virtual ~Unit() = default;
  float val;

  Unit(float inVal) {
    val = inVal;
  }
  
  virtual float Compute(float axis) const {
    return val;
  }
};

inline std::shared_ptr<Unit> operator ""_u(long double inVal){
  return std::make_shared<Unit>(inVal);
}

struct RelativeUnit final : Unit {

  RelativeUnit(float inVal) : Unit(inVal) {
    
  }
  float Compute(float axis) const  override {
    return (val / 100.0f) * axis;
  }
};

inline std::shared_ptr<RelativeUnit> operator ""_ru(long double inVal){
  return std::make_shared<RelativeUnit>(inVal);
}

}
