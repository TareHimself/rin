#include "RP3DScenePhysics.hpp"

namespace vengine {
namespace physics {
rp3d::PhysicsCommon RP3DScenePhysics::physicsCommon = rp3d::PhysicsCommon();

void RP3DScenePhysics::init(scene::Scene *outer) {
  ScenePhysics::init(outer);
  phys = physicsCommon.createPhysicsWorld();
}

void RP3DScenePhysics::onCleanup() {
  ScenePhysics::onCleanup();
  physicsCommon.destroyPhysicsWorld(phys);
  phys = nullptr;
}

void RP3DScenePhysics::fixedUpdate(float deltaTime) {
  ScenePhysics::fixedUpdate(deltaTime);
  phys->update(deltaTime);
}

}
}
