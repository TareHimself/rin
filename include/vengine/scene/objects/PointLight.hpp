#pragma once
#include "Light.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class PointLight : public Light {
  WeakPointer<BillboardComponent> _billboard = AddComponent<BillboardComponent>();
public:
  Pointer<SceneComponent> CreateRootComponent() override;
  void AttachComponentsToRoot(const WeakPointer<SceneComponent> &root) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(PointLight)
};


}
