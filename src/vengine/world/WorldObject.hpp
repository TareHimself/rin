#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace world {
class World;
}
}

namespace vengine {
namespace world {
  
/**
 * \brief Base class for all object that exist in a world
 */
class WorldObject : public Object {

  bool bCanEverUpdate = false;
  World * _world = nullptr;
public:

  World * getWorld();

  
  void setWorld(World * newWorld);
  
  virtual void update(float deltaTime);

      
};
}
}
