#include <vengine/scene/components/RenderedComponent.hpp>
#include "vengine/scene/objects//SceneObject.hpp"


namespace vengine::scene {
void RenderedComponent::Init(SceneObject * outer) {
  SceneComponent::Init(outer);
}
}
