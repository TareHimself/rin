#pragma once
#include "aerox/physics/ScenePhysics.hpp"
#include <reactphysics3d/reactphysics3d.h>


namespace aerox::physics {
class RP3DScenePhysics : public ScenePhysics {
  
  
  rp3d::PhysicsWorld * _phys = nullptr;

  
public:

  static rp3d::PhysicsCommon physicsCommon;

  void OnInit(scene::Scene * owner) override;
  void OnDestroy() override;
  
  void InternalFixedUpdate(float deltaTime) override;
};
}
