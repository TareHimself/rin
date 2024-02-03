#include <vengine/scene/objects/Light.hpp>

namespace vengine::scene {
void Light::AttachComponentsToRoot(const WeakRef<SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _billboard.Reserve()->AttachTo(root);
}
}
