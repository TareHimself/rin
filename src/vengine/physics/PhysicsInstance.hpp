#pragma once
#include "vengine/world/WorldObject.hpp"

namespace vengine {
namespace world {
class World;
}
namespace physics {
class PhysicsInstance : public world::WorldObject {
public:
  
  world::World * getWorld();
  
};
}
}
