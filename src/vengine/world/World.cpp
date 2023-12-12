#include "World.hpp"

#include "vengine/physics/rp3/RP3PhysicsInstance.hpp"

#include <iostream>
#include <vengine/physics/PhysicsInstance.hpp>
namespace vengine {
namespace world {

void World::init() {
  Object::init();
  physics = createPhysicsInstance();
  physics->setWorld(this);
  physics->init();
}

void World::destroy() {
  Object::destroy();
  physics->destroy();
  delete physics;
  physics = nullptr;
}

void World::update(float deltaTime) {
  if(physics != nullptr) {
    physics->update(0.2f);
  }
  std::cout << "World Tick Delta " << deltaTime << std::endl;
}

physics::PhysicsInstance * World::createPhysicsInstance() {
  return new physics::RP3PhysicsInstance();
}
}
}