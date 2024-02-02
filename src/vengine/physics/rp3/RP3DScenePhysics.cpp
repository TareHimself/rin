#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
#include "vengine/scene/Scene.hpp"

namespace vengine::physics {
rp3d::PhysicsCommon RP3DScenePhysics::physicsCommon = rp3d::PhysicsCommon();


void RP3DScenePhysics::Init(scene::Scene * outer) {
  ScenePhysics::Init(outer);
  _phys = physicsCommon.createPhysicsWorld();
}

void RP3DScenePhysics::HandleDestroy() {
  ScenePhysics::HandleDestroy();
  physicsCommon.destroyPhysicsWorld(_phys);
  _phys = nullptr;
}

void RP3DScenePhysics::InternalFixedUpdate(float deltaTime) {
  _phys->update(deltaTime);
}

}
