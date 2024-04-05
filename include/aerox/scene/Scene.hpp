#pragma once
#include "aerox/TOwnedBy.hpp"
#include "aerox/Object.hpp"
#include "aerox/containers/Array.hpp"
#include "aerox/containers/Serializable.hpp"
#include "aerox/input/SceneInputConsumer.hpp"
#include "aerox/math/Transform.hpp"
#include "aerox/utils.hpp"
#include "gen/scene/Scene.gen.hpp"

namespace aerox {
class Engine;
}
namespace aerox::drawing {
class SceneDrawer;
class DrawingSubsystem;
class Viewport;
}

namespace aerox::physics {
class ScenePhysics;
}

namespace aerox::scene {
class SceneObject;
class LightComponent;
class Light;

META_TYPE()
class Scene : public TOwnedBy<Engine>,public Serializable {


  std::shared_ptr<physics::ScenePhysics> _physics;
  std::shared_ptr<drawing::SceneDrawer> _drawer;
  std::shared_ptr<SceneObject> _viewTarget;
  std::shared_ptr<SceneObject> _defaultViewTarget;
  Array<std::shared_ptr<SceneObject>> _sceneObjects;
  Array<std::shared_ptr<SceneObject>> _objectsPendingInit;
  std::shared_ptr<input::SceneInputConsumer> _inputConsumer;
  std::list<std::weak_ptr<LightComponent>> _lights;
public:
  
  META_BODY()

  META_FUNCTION()
  static std::shared_ptr<Scene> Construct() {
    return newObject<Scene>();
  }
  
  Engine *GetEngine() const;

  META_PROPERTY()
  float testProp = 0.0f;
  
  void OnInit(aerox::Engine * outer) override;
  void OnDestroy() override;

  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;

  virtual bool ShouldSerializeObject(const std::shared_ptr<SceneObject> &object);

  Array<std::weak_ptr<SceneObject>> GetSceneObjects() const;

  std::list<std::weak_ptr<LightComponent>> GetSceneLights() const;

  std::weak_ptr<drawing::SceneDrawer> GetDrawer() const;

  std::weak_ptr<physics::ScenePhysics> GetPhysics() const;

  std::weak_ptr<input::SceneInputConsumer> GetInput() const;

  void RegisterLight(const std::weak_ptr<LightComponent>& light);

  /**
   * \brief Called every tick
   */
  virtual void Tick(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual std::shared_ptr<physics::ScenePhysics> CreatePhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual std::shared_ptr<drawing::SceneDrawer> CreateDrawer();

  virtual std::shared_ptr<input::SceneInputConsumer> CreateInputManager();

  virtual std::shared_ptr<SceneObject> CreateDefaultViewTarget();

  std::weak_ptr<SceneObject> GetViewTarget() const;

  virtual std::shared_ptr<SceneObject> InitSceneObject(const std::shared_ptr<SceneObject> &object);

  template <typename T,typename ... Args>
  TWeakConstruct<T,Args...> CreateSceneObject(Args &&... args);

  template <typename T,typename ... Args>
  TWeakConstruct<T,Args...> CreateSceneObject(const math::Transform &transform,Args &&... args);
};

template <typename T, typename ... Args> TWeakConstruct<T,Args...> Scene::CreateSceneObject(
    Args &&... args) {
  auto rawObj = newObject<T>(args...);
  const auto obj = utils::castStatic<SceneObject>(rawObj);
  InitSceneObject(obj);
  return rawObj;
}

template <typename T, typename ... Args> TWeakConstruct<T,Args...> Scene::CreateSceneObject(
    const math::Transform &transform, Args &&... args) {
  auto rawObj = CreateSceneObject<T>(args...);
  const auto obj = utils::castStatic<SceneObject>(rawObj);
  obj->SetWorldTransform(transform);
  return rawObj;
}
}
