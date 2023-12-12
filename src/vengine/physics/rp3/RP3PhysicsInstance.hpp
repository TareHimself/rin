#pragma once
#include <reactphysics3d/reactphysics3d.h>
#include <vengine/physics/PhysicsInstance.hpp>

namespace vengine {
namespace physics {
class RP3PhysicsInstance : public PhysicsInstance {
  
  
  rp3d::PhysicsWorld * phys = nullptr;
public:

  static rp3d::PhysicsCommon physicsCommon;
  
  void init() override;

  void destroy() override;

  void update(float deltaTime) override;
};
}
}
