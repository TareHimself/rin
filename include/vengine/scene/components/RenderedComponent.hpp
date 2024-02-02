#pragma once
#include "Component.hpp"
#include "SceneComponent.hpp"
#include "vengine/drawing/scene/SceneDrawable.hpp"


namespace vengine::scene {
class RenderedComponent : public SceneComponent,public SharableThis<RenderedComponent>, public drawing::SceneDrawable {
  void Init(SceneObject * outer) override;
};
}
