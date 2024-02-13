#pragma once
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/BillboardComponent.hpp"

namespace vengine::scene {
class Light : public SceneObject {
protected:
  Ref<BillboardComponent> _billboard = AddComponent<BillboardComponent>();

public:
  virtual void SetIntensity(float val);
  virtual float GetIntensity() const;

  virtual void SetColor(const glm::vec4& color);
  virtual glm::vec4 GetColor() const;
  void AttachComponentsToRoot(const Ref<SceneComponent> &root) override;
};


}
