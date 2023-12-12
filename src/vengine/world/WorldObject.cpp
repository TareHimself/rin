#include "WorldObject.hpp"

namespace vengine {
namespace world {

World * WorldObject::getWorld() {
  return _world;
}

void WorldObject::setWorld(World * newWorld) {
  _world = newWorld;
}

void WorldObject::update(float deltaTime) {
  
}

}
}
