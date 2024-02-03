#pragma once
#include "Light.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class DirectionalLight : public Light {
public:
  Ref<SceneComponent> CreateRootComponent() override;
  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(DirectionalLight)
};


}
