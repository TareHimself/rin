#pragma once
#include "types.hpp"

namespace vengine::drawing {
class DrawingSubsystem;

class Drawer {
public:
  
  virtual void Draw(RawFrameData * frameData) = 0;
};
}


