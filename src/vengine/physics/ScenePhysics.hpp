#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class Scene;
}
namespace physics {
class ScenePhysics : public Object {

  scene::Scene * _world = nullptr;
public:
  

  scene::Scene * getWorld();

  void setWorld(scene::Scene * newWorld);
  
  virtual void fixedUpdate(float deltaTime);
  
};
}
}
