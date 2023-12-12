#include "RP3PhysicsInstance.hpp"

namespace vengine {
namespace physics {
rp3d::PhysicsCommon RP3PhysicsInstance::physicsCommon = rp3d::PhysicsCommon();

void RP3PhysicsInstance::init() {
  PhysicsInstance::init();
  phys = physicsCommon.createPhysicsWorld();
}

void RP3PhysicsInstance::destroy() {
  PhysicsInstance::destroy();
  physicsCommon.destroyPhysicsWorld(phys);
  phys = nullptr;
}

void RP3PhysicsInstance::update(float deltaTime) {
  PhysicsInstance::update(deltaTime);
  phys->update(deltaTime);
}

}
}
