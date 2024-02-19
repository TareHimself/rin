#pragma once
#include "types.hpp"

namespace vengine::drawing {
class SceneDrawer;
}

namespace vengine::drawing {
class SceneDrawable {
public:
  
  virtual void Draw(SceneFrameData *frameData, const math::Transform &parentTransform) = 0;
};
}
