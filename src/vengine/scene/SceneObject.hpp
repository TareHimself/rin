#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class Scene;
}
}

namespace vengine {
namespace scene {
  
/**
 * \brief Base class for all object that exist in a world
 */
class SceneObject : public Object {

  bool bCanEverUpdate = false;
  Scene * _world = nullptr;
public:

  Scene * getScene();

  

  
  void setWorld(Scene * newWorld);
  
  virtual void update(float deltaTime);

      
};
}
}
