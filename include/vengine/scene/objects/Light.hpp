#pragma once
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class Light : public SceneObject {
protected:
  WeakRef<BillboardComponent> _billboard = AddComponent<BillboardComponent>();
public:

void AttachComponentsToRoot(const WeakRef<SceneComponent> &root) override;
  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(Light)
};


}