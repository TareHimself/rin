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
class DrawingSubsystem;
}

namespace vengine::drawing {
class SceneDrawer : public Object<scene::Scene>, public Drawable {
  
  SceneGlobalBuffer _sceneData{};
  Managed<AllocatedBuffer> _sceneGlobalBuffer;
  Managed<MaterialInstance> _defaultCheckeredMaterial;

public:
  virtual Ref<DrawingSubsystem> GetDrawer();

  void Init(scene::Scene * outer) override;
  void Draw(RawFrameData *frameData) override;

  void BeforeDestroy() override;

  Ref<MaterialInstance> GetDefaultMaterial() const;
};
}
