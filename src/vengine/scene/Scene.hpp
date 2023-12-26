#pragma once
#include "SceneObject.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace rendering {
class Renderer;
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
  class Scene : public Object<Engine> {
    physics::ScenePhysics * physics = nullptr;
    CameraComponent * activeCamera = nullptr;
    Array<SceneObject *> objects;
    
  public:
      Engine *getEngine() const;
    
      void init(Engine *outer) override;
      void onCleanup() override;

      virtual void render(rendering::Renderer * renderer,const vk::CommandBuffer *cmd);
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
