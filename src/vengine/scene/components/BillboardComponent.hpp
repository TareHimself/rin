#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Texture.hpp"

namespace vengine::scene {
class BillboardComponent : public RenderedComponent {
  drawing::Texture * _texture = nullptr;
public:
  virtual void Draw(drawing::SceneDrawer *drawer, drawing::SceneFrameData *frameData) override;
};
}
