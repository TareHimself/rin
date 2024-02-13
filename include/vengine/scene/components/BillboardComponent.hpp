#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Texture.hpp"
#include "generated/scene/components/BillboardComponent.reflect.hpp"

namespace vengine::scene {
RCLASS()
class BillboardComponent : public RenderedComponent {
  Managed<drawing::Texture> _texture;
public:
  virtual void Draw(drawing::SimpleFrameData *frameData, const math::Transform &parentTransform) override;

  RFUNCTION()
  static Managed<BillboardComponent> Construct() { return newManagedObject<BillboardComponent>(); } VENGINE_IMPLEMENT_REFLECTED_INTERFACE(BillboardComponent)
};

REFLECT_IMPLEMENT(BillboardComponent)
}
