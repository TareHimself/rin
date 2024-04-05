#include <aerox/physics/rp3/RP3DScenePhysics.hpp>
#include "aerox/scene/Scene.hpp"

namespace aerox::physics {
rp3d::PhysicsCommon RP3DScenePhysics::physicsCommon = rp3d::PhysicsCommon();

void RP3DScenePhysics::OnInit(scene::Scene * owner) {
  ScenePhysics::OnInit(owner);
  //_phys = physicsCommon.createPhysicsWorld();
}

void RP3DScenePhysics::OnDestroy() {
  ScenePhysics::OnDestroy();
  //physicsCommon.destroyPhysicsWorld(_phys);
  //_phys = nullptr;
}

void RP3DScenePhysics::InternalFixedUpdate(float deltaTime) {
  //_phys->update(deltaTime);
}

}
