#include "SceneObject.hpp"
#include <vengine/scene/Scene.hpp>
#include "components/RenderedComponent.hpp"

namespace vengine {
namespace scene {

// void SceneObject::setWorld(Scene * newWorld) {
//   _world = newWorld;
// }

void SceneObject::update(float deltaTime) {
  
}

void SceneObject::render(rendering::Renderer *renderer,
    const vk::CommandBuffer *cmd) {
  for(const auto comp : renderedComponents) {
    comp->render(renderer,cmd);
  }
}

}
}
