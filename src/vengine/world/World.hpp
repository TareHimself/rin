#pragma once
#include "vengine/Object.hpp"

namespace vengine {
namespace physics {
class PhysicsInstance;
}

namespace world {

/**
 * \brief Base class for worlds
 */
  class World : public Object {
      physics::PhysicsInstance * physics = nullptr;

  public:
      void init() override;
      void destroy() override;

      /**
       * \brief Called every tick
       */
      virtual void update(float deltaTime);

      
      /**
       * \brief Create a new physics instance for this world
       * \return The created instance
       */
      virtual physics::PhysicsInstance * createPhysicsInstance();
  };
}
}
