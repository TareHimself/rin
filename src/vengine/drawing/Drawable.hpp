#pragma once
#include "types.hpp"

namespace vengine {
namespace drawing {
class Drawer;

class Drawable {
public:
  
  virtual void draw(Drawer * drawer,FrameData * frameData) = 0;
};
}
}


