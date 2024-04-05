#pragma once
#include "types.hpp"
#include "aerox/Object.hpp"
#include "aerox/drawing/Drawer.hpp"
#include "aerox/drawing/MaterialInstance.hpp"

namespace aerox::scene {
class Scene;
}

namespace aerox::drawing {
class SceneDrawable;
}

namespace aerox::drawing {
class DrawingSubsystem;
}

namespace aerox::drawing {

class SceneDrawer : public TOwnedBy<scene::Scene>, public Drawer {

protected:
  vk::Extent2D _drawExtent;
  std::weak_ptr<WindowDrawer> _windowDrawer;
public:
  virtual std::weak_ptr<DrawingSubsystem> GetDrawer();

  void OnInit(scene::Scene * owner) override;
  
  //void Draw(RawFrameData *frameData) override;

  //void OnDestroy() override;

  virtual vk::Extent2D GetDrawExtent() const;

  virtual std::weak_ptr<WindowDrawer> GetWindowDrawer();

  virtual std::weak_ptr<MaterialInstance> GetDefaultMaterial() = 0;

  virtual Array<vk::Format> GetColorAttachmentFormats() = 0;

  virtual std::shared_ptr<MaterialInstance> CreateMaterialInstance(const Array<std::shared_ptr<Shader>> &shaders) = 0;

  virtual std::weak_ptr<AllocatedImage> GetRenderTarget() = 0;
  
};
}
