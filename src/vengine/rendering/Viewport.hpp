#pragma once
#include "vengine/Object.hpp"
#include "vengine/world/WorldObject.hpp"

#include <reactphysics3d/reactphysics3d.h>

namespace vengine {
namespace world {
class World;
}
}

namespace vengine {
namespace rendering {

/**
 * \brief Base class for a viewport belonging to a world
 */
  class Viewport : public world::WorldObject {

    rp3d::Vector2 size;
  public:

      Viewport();

      rp3d::Vector2 getViewportSize();
      void setViewportSize(rp3d::Vector2 newSize);
    
      void init() override;
      void destroy() override;
    
      virtual void update(float deltaTime);
  };
}
}
