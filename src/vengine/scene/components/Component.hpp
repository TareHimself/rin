#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace scene {
class SceneObject;
}
}

namespace vengine {
namespace scene {
class Component : public Object{

  SceneObject * owner = nullptr;
public:
  void init() override;

  virtual void init(SceneObject * owningObject);

  SceneObject * getOwner();

  virtual void update(float deltaTime);
};
}
}
