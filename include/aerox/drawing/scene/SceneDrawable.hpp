#pragma once
#include "types.hpp"

namespace aerox::drawing {
class SceneDrawer;
}

namespace aerox::drawing {
class SceneDrawable {
public:
  
  virtual void Draw(SceneFrameData *frameData, const math::Transform &parentTransform) = 0;
};
}
