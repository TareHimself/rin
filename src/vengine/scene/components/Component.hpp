#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class SceneObject;
}
}

namespace vengine {
namespace scene {
class Component : public Object<SceneObject>{

  SceneObject * owner = nullptr;
public:
  virtual void init(SceneObject * owner) override;

  virtual void update(float deltaTime);
};
}
}
