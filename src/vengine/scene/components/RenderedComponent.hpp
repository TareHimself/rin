#pragma once
#include "Component.hpp"
#include "SceneComponent.hpp"
#include "vengine/drawing/scene/SceneDrawable.hpp"


namespace vengine::scene {
class RenderedComponent : public SceneComponent, public drawing::SceneDrawable {
  virtual void Init(SceneObject *outer) override;
  virtual void HandleDestroy() override;
};
}
