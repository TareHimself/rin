#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Texture.hpp"

namespace vengine::scene {
class BillboardComponent : public RenderedComponent {
  Ref<drawing::Texture> _texture;
public:
  virtual void Draw(drawing::SceneDrawer *drawer, const math::Transform &parentTransform, drawing::SimpleFrameData *frameData) override;

  VENGINE_IMPLEMENT_COMPONENT_ID(BillboardComponent)
};


}
