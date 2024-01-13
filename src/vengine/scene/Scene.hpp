#pragma once
#include "SceneObject.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/drawing/Drawable.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace drawing {
class SceneDrawer;
}
}

namespace vengine {
namespace drawing {
class Drawer;
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
  physics::ScenePhysics *_physics = nullptr;
  drawing::SceneDrawer *_drawer = nullptr;
  CameraComponent *activeCamera = nullptr;
  Array<SceneObject *> objects;
  Array<SceneObject *> objectsPendingInit;

public:
  Engine *getEngine() const;

  void init(Engine *outer) override;
  void handleCleanup() override;

  Array<SceneObject *> getSceneObjects() const;

  drawing::SceneDrawer * getDrawer() const;

  physics::ScenePhysics * getPhysics() const;

  /**
   * \brief Called every tick
   */
  virtual void update(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual physics::ScenePhysics *createPhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual drawing::SceneDrawer * createDrawer();

  virtual SceneObject * initObject(SceneObject * object);

  template <typename T>
  T *createSceneObject();

  template <typename T>
  T *createSceneObject(const math::Transform &transform);
};

template <typename T> T *Scene::createSceneObject() {
  auto rawObj = newObject<T>();
  const auto obj = dynamic_cast<SceneObject *>(rawObj);
  initObject(obj);
  return rawObj;
}

template <typename T> T *Scene::
createSceneObject(const math::Transform &transform) {
  auto rawObj = createSceneObject<T>();
  const auto obj = dynamic_cast<SceneObject *>(rawObj);
  obj->setTransform(transform);
  return rawObj;
}
}
}
