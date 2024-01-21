#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/drawing/Drawable.hpp"

namespace vengine::scene {
class Scene;
}

namespace vengine::drawing {
class SceneDrawable;
}

namespace vengine::drawing {
class Drawer;
}

namespace vengine::drawing {
class SceneDrawer : public Object<scene::Scene>, public Drawable {
  vk::Viewport _viewport;
  SceneGpuData _sceneData{};
  std::optional<AllocatedBuffer> _gpuSceneDataBuffer;
public:
  virtual Drawer * GetEngineRenderer();

  void Init(scene::Scene *outer) override;
  void Draw(Drawer *drawer, FrameData *frameData) override;

  void HandleDestroy() override;
};
}
