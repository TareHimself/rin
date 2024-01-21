#pragma once
#include "SceneObject.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/input/SceneInputManager.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine::drawing {
class SceneDrawer;
class Drawer;
class Viewport;
}

namespace vengine {
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
  SceneObject * _viewTarget = nullptr;
  SceneObject * _defaultViewTarget = nullptr;
  Array<SceneObject *> _sceneObjects;
  Array<SceneObject *> _objectsPendingInit;
  input::SceneInputManager * _inputManager = nullptr;

public:
  Engine *  GetEngine() const;

  void Init(Engine *outer) override;
  void HandleDestroy() override;

  Array<SceneObject *> GetSceneObjects() const;

  drawing::SceneDrawer *  GetDrawer() const;

  physics::ScenePhysics *  GetPhysics() const;

  input::SceneInputManager *  GetInput() const;

  /**
   * \brief Called every tick
   */
  virtual void Update(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual physics::ScenePhysics *  CreatePhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual drawing::SceneDrawer * CreateDrawer();

  virtual input::SceneInputManager * CreateInputManager();

  virtual SceneObject *  CreateDefaultViewTarget();

  SceneObject *  GetViewTarget() const;

  virtual SceneObject * InitSceneObject(SceneObject * object);

  template <typename T,typename ... Args>
  T * CreateSceneObject(Args &&... args);

  template <typename T,typename ... Args>
  T * CreateSceneObject(const math::Transform &transform,Args &&... args);
};

template <typename T, typename ... Args> T * Scene::CreateSceneObject(
    Args &&... args) {
  auto rawObj = newObject<T>(args...);
  const auto obj = dynamic_cast<SceneObject *>(rawObj);
  InitSceneObject(obj);
  return rawObj;
}

template <typename T, typename ... Args> T * Scene::CreateSceneObject(
    const math::Transform &transform, Args &&... args) {
  auto rawObj = CreateSceneObject<T>(args...);
  const auto obj = dynamic_cast<SceneObject *>(rawObj);
  obj->SetWorldTransform(transform);
  return rawObj;
}
}
}
