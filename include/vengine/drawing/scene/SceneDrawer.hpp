#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

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
  SceneGlobalBuffer _sceneData{};
  Ref<AllocatedBuffer> _sceneGlobalBuffer;
  Ref<MaterialInstance> _defaultCheckeredMaterial;

public:
  virtual WeakRef<Drawer> GetDrawer();

  void Init(scene::Scene * outer) override;
  void Draw(Drawer * drawer, RawFrameData *frameData) override;

  void HandleDestroy() override;

  WeakRef<MaterialInstance> GetDefaultMaterial() const;
};
}
