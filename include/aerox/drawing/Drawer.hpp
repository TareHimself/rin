#pragma once
#include "types.hpp"

namespace aerox::drawing {
class DrawingSubsystem;

class Drawer {
public:
  
  virtual void Draw(RawFrameData * frameData) = 0;
};
}


