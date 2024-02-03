#pragma once
#include "types.hpp"

namespace vengine::drawing {
class SceneDrawer;
}

namespace vengine::drawing {
class SceneDrawable {
public:
  virtual void Draw(SceneDrawer *drawer, const math::Transform &parentTransform, SimpleFrameData *frameData) = 0;
};
}
