#pragma once
#include "SceneObject.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace rendering {
class Viewport;
}

class Engine;

namespace physics {
class ScenePhysics;
}

namespace scene {
class CameraComponent;

/**
 * \brief Base class for worlds
 */
  class Scene : public Object {
    physics::ScenePhysics * physics = nullptr;
    Engine * _engine = nullptr;
    CameraComponent * activeCamera = nullptr;
    Array<SceneObject> objects;
    
  public:
      void setEngine(Engine *newEngine);
      Engine *getEngine();
    
      void init() override;
      void destroy() override;

      virtual void render(const vk::CommandBuffer *cmd);
      /**
       * \brief Called every tick
       */
      virtual void update(float deltaTime);
    
      /**
       * \brief Create a new physics instance for this world
       * \return The created instance
       */
      virtual physics::ScenePhysics * createPhysicsInstance();
  };
}
}
