#pragma once
#include "vengine/scene/objects/PointLight.hpp"
#include "vengine/scene/objects/SceneObject.hpp"

using namespace vengine;

class LightArray : public scene::SceneObject {
public:
  Array<Ref<scene::PointLight>> lights;

  void Init(scene::Scene *outer) override;
  void Tick(float deltaTime) override;
};
