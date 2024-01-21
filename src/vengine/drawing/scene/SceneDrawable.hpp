#pragma once
#include "types.hpp"

namespace vengine::drawing {
class SceneDrawer;
}

namespace vengine::drawing {
class SceneDrawable {
public:
  virtual void Draw(SceneDrawer * drawer,SceneFrameData * frameData) = 0;
};
}
