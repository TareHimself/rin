#pragma once
#include "types.hpp"

namespace vengine::drawing {
class Drawer;

class Drawable {
public:
  
  virtual void Draw(Drawer * drawer,FrameData * frameData) = 0;
};
}


