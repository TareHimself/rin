#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Texture2D.hpp"
#include "generated/scene/components/BillboardComponent.reflect.hpp"

namespace vengine::scene {
RCLASS()
class BillboardComponent : public RenderedComponent {
  Managed<drawing::Texture2D> _texture;
public:
  virtual void Draw(drawing::SceneFrameData *frameData, const math::Transform &parentTransform) override;

  RFUNCTION()
  static Managed<BillboardComponent> Construct() { return newManagedObject<BillboardComponent>(); } VENGINE_IMPLEMENT_REFLECTED_INTERFACE(BillboardComponent)
};

REFLECT_IMPLEMENT(BillboardComponent)
}
