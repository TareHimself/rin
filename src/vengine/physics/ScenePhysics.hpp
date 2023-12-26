#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class Scene;
}
namespace physics {
class ScenePhysics : public Object<scene::Scene> {
public:
  
  virtual void fixedUpdate(float deltaTime);
  
};
}
}
