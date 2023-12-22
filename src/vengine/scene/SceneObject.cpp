#include "SceneObject.hpp"

namespace vengine {
namespace scene {

Scene * SceneObject::getScene() {
  return _world;
}

void SceneObject::setWorld(Scene * newWorld) {
  _world = newWorld;
}

void SceneObject::update(float deltaTime) {
  
}

}
}
