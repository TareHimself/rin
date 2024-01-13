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
public:
  SceneObject * getOwner() const;
};
}
}
