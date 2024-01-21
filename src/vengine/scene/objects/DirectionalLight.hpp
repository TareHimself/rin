#pragma once
#include "vengine/scene/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class DirectionalLight : public SceneObject {
  BillboardComponent * _billboard = AddComponent<BillboardComponent>();
public:
  SceneComponent *CreateRootComponent() override;
  void AttachComponentsToRoot(SceneComponent *root) override;
};
}
