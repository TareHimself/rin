#pragma once
#include "vengine/physics/ScenePhysics.hpp"
#include <reactphysics3d/reactphysics3d.h>


namespace vengine {
namespace physics {
class RP3DScenePhysics : public ScenePhysics {
  
  
  rp3d::PhysicsWorld * phys = nullptr;
public:

  static rp3d::PhysicsCommon physicsCommon;
  
  void init(scene::Scene *outer) override;

  void onCleanup() override;

  void fixedUpdate(float deltaTime) override;
  
};
}
}
