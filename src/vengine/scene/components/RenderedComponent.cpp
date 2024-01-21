#include "RenderedComponent.hpp"

#include "vengine/scene/SceneObject.hpp"


namespace vengine::scene {


void RenderedComponent::Init(SceneObject *outer) {
  SceneComponent::Init(outer);
  outer->AddToRenderList(this);
}

void RenderedComponent::HandleDestroy() {
  SceneComponent::HandleDestroy();
  GetOuter()->RemoveFromRenderList(this);
}
}
