#pragma once
#include "aerox/scene/objects/PointLight.hpp"
#include "aerox/scene/objects/SceneObject.hpp"

using namespace aerox;

class LightArray : public scene::SceneObject {
public:
  Array<std::weak_ptr<scene::PointLight>> lights;

  void OnInit(scene::Scene * scene) override;
  void Tick(float deltaTime) override;

  std::shared_ptr<meta::Metadata> GetMeta() const override{
      return {};
  }
};
