#pragma once
#include "vengine/Object.hpp"
#include "vengine/drawing/Drawable.hpp"

namespace vengine {
namespace scene {
class Scene;
}
}

namespace vengine {
namespace drawing {
class SceneDrawable;
}
}

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine {
namespace drawing {
class SceneDrawer : public Object<scene::Scene>, public Drawable {
  vk::Viewport _viewport;
  GPUSceneData _sceneData;
public:
  virtual Drawer * getEngineRenderer();

  void init(scene::Scene *outer) override;
  void draw(Drawer *drawer, FrameData *frameData) override;
};
}
}
