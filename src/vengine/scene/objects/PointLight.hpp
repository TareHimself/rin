#pragma once
#include "vengine/scene/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class PointLight : public SceneObject {
  BillboardComponent * _billboard = AddComponent<BillboardComponent>();
public:
  SceneComponent *CreateRootComponent() override;
  void AttachComponentsToRoot(SceneComponent *root) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(PointLight)
};


}
