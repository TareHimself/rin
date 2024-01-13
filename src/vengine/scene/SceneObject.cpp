#include "SceneObject.hpp"
#include <vengine/scene/Scene.hpp>
#include "components/RenderedComponent.hpp"

namespace vengine {
namespace scene {

// void SceneObject::setWorld(Scene * newWorld) {
//   _world = newWorld;
// }

SceneComponent * SceneObject::createRootComponent() {
  return newObject<SceneComponent>();
}

SceneComponent * SceneObject::getRootComponet() {
  return _rootComponent;
}

void SceneObject::attachComponentsToRoot(SceneComponent *root) {
  /* Attach scene components here */
}

math::Transform SceneObject::getTransform() const {
  return _rootComponent->getRelativeTransform();
}

void SceneObject::setTransform(const math::Transform &transform) const {
  _rootComponent->setRelativeTransform(transform);
}

void SceneObject::init(Scene *outer) {
  Object<Scene>::init(outer);
  _rootComponent = createRootComponent();
  _rootComponent->init(this);
  attachComponentsToRoot(_rootComponent);
  for(const auto component : _components) {
    component->init(this);
  }
}

void SceneObject::update(float deltaTime) {
  
}

void SceneObject::handleCleanup() {
  Object<Scene>::handleCleanup();
  _renderedComponents.clear();
  
  for(const auto component : _components) {
    component->cleanup();
  }
  
  _components.clear();

  _rootComponent->cleanup();
  _rootComponent = nullptr;
}

void SceneObject::draw(drawing::SceneDrawer *renderer,
    drawing::SceneFrameData *frameData) {
  for(const auto comp : _renderedComponents) {
    comp->draw(renderer,frameData);
  }
}

}
}
