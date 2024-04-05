#include <aerox/physics/ScenePhysics.hpp>
#include <aerox/physics/constants.hpp>


namespace aerox::physics {

void ScenePhysics::FixedUpdate(float deltaTime) {
  _accum += deltaTime;
  while(_accum >= PHYSICS_FIXED_UPDATE) {
    InternalFixedUpdate(PHYSICS_FIXED_UPDATE);
    _accum -= PHYSICS_FIXED_UPDATE;
  }
}
}
