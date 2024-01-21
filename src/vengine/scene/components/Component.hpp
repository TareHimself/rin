#pragma once
#include "vengine/Object.hpp"

namespace vengine::scene {
class SceneObject;
}

namespace vengine::scene {
class Component : public Object<SceneObject> {
public:
  SceneObject * GetOwner() const;
};
}
