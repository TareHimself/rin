#include <vengine/physics/ScenePhysics.hpp>
#include <vengine/physics/constants.hpp>


namespace vengine::physics {

void ScenePhysics::FixedUpdate(float deltaTime) {
  _accum += deltaTime;
  while(_accum >= PHYSICS_FIXED_UPDATE) {
    InternalFixedUpdate(PHYSICS_FIXED_UPDATE);
    _accum -= PHYSICS_FIXED_UPDATE;
  }
}
}
