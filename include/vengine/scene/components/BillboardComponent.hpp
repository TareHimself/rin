#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Texture.hpp"

namespace vengine::scene {
class BillboardComponent : public RenderedComponent {
  Pointer<drawing::Texture> _texture;
public:
  virtual void Draw(drawing::SceneDrawer *drawer, drawing::SimpleFrameData *frameData) override;

  VENGINE_IMPLEMENT_COMPONENT_ID(BillboardComponent)
};


}
