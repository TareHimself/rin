#pragma once
#include "aerox/Object.hpp"
#include "aerox/TObjectWithInit.hpp"
#include "aerox/TOwnedBy.hpp"

namespace aerox {
namespace scene {
class Scene;
}

namespace physics {
class ScenePhysics : public TOwnedBy<scene::Scene> {
  float _accum = 0.0f;

public:
  
  virtual void FixedUpdate(float deltaTime);

  virtual void InternalFixedUpdate(float deltaTime) = 0;
};
}
}
