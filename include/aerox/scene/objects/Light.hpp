#pragma once
#include "aerox/scene/objects/SceneObject.hpp"
#include "aerox/scene/components/BillboardComponent.hpp"
#include "gen/scene/objects/Light.gen.hpp"

namespace aerox::scene {

META_TYPE()
class Light : public SceneObject {
protected:
  std::weak_ptr<BillboardComponent> _billboard = AddComponent<BillboardComponent>();

public:
  META_BODY()
  virtual void SetIntensity(float val);
  virtual float GetIntensity() const;

  virtual void SetColor(const glm::vec4& color);
  virtual glm::vec4 GetColor() const;
  void AttachComponentsToRoot(const std::weak_ptr<SceneComponent> &root) override;
};

}
