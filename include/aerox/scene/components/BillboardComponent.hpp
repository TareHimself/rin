#pragma once
#include "RenderedComponent.hpp"
#include "aerox/drawing/Texture.hpp"
#include "gen/scene/components/BillboardComponent.gen.hpp"

namespace aerox::scene {
META_TYPE()
class BillboardComponent : public RenderedComponent {
  std::shared_ptr<drawing::Texture> _texture;
public:

  META_BODY()
  
  virtual void Draw(drawing::SceneFrameData *frameData, const math::Transform &parentTransform) override;

  META_FUNCTION()
  static std::shared_ptr<BillboardComponent> Construct() { return newObject<BillboardComponent>(); }
};
}
