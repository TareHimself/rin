#pragma once
#include "vengine/physics/ScenePhysics.hpp"
#include <reactphysics3d/reactphysics3d.h>


namespace vengine::physics {
class RP3DScenePhysics : public ScenePhysics {
  
  
  rp3d::PhysicsWorld * _phys = nullptr;

  
public:

  static rp3d::PhysicsCommon physicsCommon;

  void Init(scene::Scene * outer) override;
  void HandleDestroy() override;
  
  void InternalFixedUpdate(float deltaTime) override;
};
}
