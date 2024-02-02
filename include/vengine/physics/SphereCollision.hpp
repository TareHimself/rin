#pragma once
#include "vengine/scene/components/RenderedComponent.hpp"

namespace vengine::physics {
class SphereCollision : public scene::RenderedComponent  {
  
public:
  virtual void SetRadius(float val) = 0;
  virtual float GetRadius() = 0;
};
}
