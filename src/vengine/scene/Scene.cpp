#include "Scene.hpp"
#include "vengine/Engine.hpp"
#include "vengine/input/InputManager.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/physics/rp3/RP3DScenePhysics.hpp"
#include <vengine/scene/SceneObject.hpp>

namespace vengine {
namespace scene {


Engine * Scene::getEngine() const {
  return getOuter();
}

void Scene::init(Engine *outer) {
  Object::init(outer);
  physics = createPhysicsInstance();
  physics->init(this);
  auto unsubscribe = getEngine()->getInputManager()->onReleased(SDLK_1,[=] (const input::KeyInputEvent &e){
    log::engine->info("Key Pressed " + e.getName());
    return false;
  });
}

void Scene::onCleanup() {
  Object::onCleanup();
  physics->cleanup();
  physics = nullptr;
}

void Scene::render(rendering::Renderer * renderer,const vk::CommandBuffer *cmd) {
  for(const auto object : objects) {
    object->render(renderer,cmd);
  }
}

void Scene::update(float deltaTime) {
  if(physics != nullptr) {
    physics->fixedUpdate(0.2f);
  }
}

physics::ScenePhysics * Scene::createPhysicsInstance() {
  return newObject<physics::RP3DScenePhysics>();
}
}
}