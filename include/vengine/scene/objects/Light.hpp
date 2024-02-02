#pragma once
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class Light : public SceneObject {
  WeakPointer<BillboardComponent> _billboard = AddComponent<BillboardComponent>();
public:
  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(Light)
};


}