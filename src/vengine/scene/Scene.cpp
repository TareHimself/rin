#include "Scene.hpp"
#include "vengine/Engine.hpp"
#include "vengine/physics/rp3/RP3DScenePhysics.hpp"
#include <iostream>

namespace vengine {
namespace scene {


void Scene::setEngine(Engine *newEngine) {
  _engine = newEngine;
}

Engine * Scene::getEngine() {
  return _engine;
}

void Scene::init() {
  Object::init();
  physics = createPhysicsInstance();
  physics->setWorld(this);
  physics->init();
}

void Scene::destroy() {
  Object::destroy();
  physics->destroy();
  delete physics;
  physics = nullptr;
}

void Scene::render(const vk::CommandBuffer *cmd) {
  
}

void Scene::update(float deltaTime) {
  if(physics != nullptr) {
    physics->fixedUpdate(0.2f);
  }
  std::cout << "World Tick Delta " << deltaTime << std::endl;
}

physics::ScenePhysics * Scene::createPhysicsInstance() {
  return new physics::RP3DScenePhysics();
}
}
}