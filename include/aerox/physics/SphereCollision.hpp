#pragma once
#include "aerox/scene/components/RenderedComponent.hpp"

namespace aerox::physics {
class SphereCollision : public scene::RenderedComponent  {
  
public:
  virtual void SetRadius(float val) = 0;
  virtual float GetRadius() = 0;
};
}
