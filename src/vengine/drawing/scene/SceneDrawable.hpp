#pragma once
#include "types.hpp"

namespace vengine {
namespace drawing {
class SceneDrawer;
}
}

namespace vengine {
namespace drawing {
class SceneDrawable {
public:
  virtual void draw(SceneDrawer * drawer,SceneFrameData * frameData) = 0;
};
}
}
