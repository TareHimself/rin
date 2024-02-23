#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/drawing/Drawer.hpp"
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

class SceneDrawer : public Object<scene::Scene>, public Drawer {

  Managed<MaterialInstance> _defaultCheckeredMaterial;
  vk::Extent2D _drawExtent;
  Ref<WindowDrawer> _windowDrawer;
protected:
  SceneGlobalBuffer _sceneData{};
  Managed<AllocatedBuffer> _sceneGlobalBuffer;
  
public:
  virtual Ref<DrawingSubsystem> GetDrawer();

  void Init(scene::Scene * outer) override;
  
  void Draw(RawFrameData *frameData) override;

  void BeforeDestroy() override;

  virtual vk::Extent2D GetDrawExtent() const;

  virtual Ref<WindowDrawer> GetWindowDrawer();

  Ref<MaterialInstance> GetDefaultMaterial() const;

  virtual Array<vk::Format> GetColorAttachmentFormats() = 0;
  Managed<MaterialInstance> CreateMaterialInstance(const Array<Managed<Shader>> &shaders);

  virtual Ref<AllocatedImage> GetRenderTarget() = 0;
  
};
}
