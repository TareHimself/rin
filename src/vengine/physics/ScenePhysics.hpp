#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class Scene;
}
namespace physics {
class ScenePhysics : public Object<scene::Scene> {
  float _accum = 0.0f;

protected:
  
public:
  
  virtual void FixedUpdate(float deltaTime);

  virtual void InternalFixedUpdate(float deltaTime) = 0;
};
}
}
