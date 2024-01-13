#include "Scene.hpp"
#include "vengine/Engine.hpp"
#include "vengine/input/InputManager.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/physics/rp3/RP3DScenePhysics.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"

#include <vengine/scene/SceneObject.hpp>

namespace vengine {
namespace scene {


Engine * Scene::getEngine() const {
  return getOuter();
}

void Scene::init(Engine *outer) {
  Object::init(outer);
  if(!objectsPendingInit.empty()) {
    for(const auto obj : objectsPendingInit) {
      initObject(obj);
    }
    
    objectsPendingInit.clear();
  }
  _physics = createPhysics();
  _physics->init(this);
  _drawer = createDrawer();
  _drawer->init(this);

  // Input Test
  auto unsubscribe = getEngine()->getInputManager()->onReleased(SDLK_1,[=] (const input::KeyInputEvent &e){
    log::engine->info("Key Pressed " + e.getName());
    return false;
  });
}

void Scene::handleCleanup() {
  Object::handleCleanup();
  for(const auto object : objects) {
    object->cleanup();
  }

  objects.clear();

  _drawer->cleanup();
  _drawer = nullptr;
  
  _physics->cleanup();
  _physics = nullptr;
}

Array<SceneObject *> Scene::getSceneObjects() const {
  return objects;
}

drawing::SceneDrawer * Scene::getDrawer() const {
  return _drawer;
}

physics::ScenePhysics * Scene::getPhysics() const {
  return _physics;
}

void Scene::update(float deltaTime) {
  if(_physics != nullptr) {
    _physics->fixedUpdate(0.2f);
  }
  for(const auto obj : objects) {
    obj->update(deltaTime);
  }
}

physics::ScenePhysics * Scene::createPhysics() {
  return newObject<physics::RP3DScenePhysics>();
}

drawing::SceneDrawer * Scene::createDrawer() {
  return newObject<drawing::SceneDrawer>();
}

SceneObject * Scene::initObject(SceneObject *object) {
  if (hasBeenInitialized()) {
    objects.push(object);
    object->init(this);
  } else {
    objectsPendingInit.push(object);
  }
  return object;
}
}
}