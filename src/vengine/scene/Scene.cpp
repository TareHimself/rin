#include "Scene.hpp"
#include "objects/DefaultCamera.hpp"
#include "vengine/Engine.hpp"
#include "vengine/input/InputManager.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/physics/rp3/RP3DScenePhysics.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"

#include <vengine/scene/SceneObject.hpp>

namespace vengine::scene {


Engine * Scene::GetEngine() const {
  return GetOuter();
}

void Scene::Init(Engine *outer) {
  Object::Init(outer);
  _inputManager = CreateInputManager();
  _inputManager->Init(GetEngine());
  _physics = CreatePhysics();
  _physics->Init(this);
  _drawer = CreateDrawer();
  _drawer->Init(this);
  
  _defaultViewTarget = InitSceneObject(CreateDefaultViewTarget());
  
  if(!_objectsPendingInit.empty()) {
    for(const auto obj : _objectsPendingInit) {
      InitSceneObject(obj);
    }
    
    _objectsPendingInit.clear();
  }
  
}

void Scene::HandleDestroy() {
  Object::HandleDestroy();
  for(const auto object : _sceneObjects) {
    object->Destroy();
  }

  _sceneObjects.clear();

  _drawer->Destroy();
  _drawer = nullptr;
  
  _physics->Destroy();
  _physics = nullptr;

  _inputManager->Destroy();
  _inputManager = nullptr;
}

Array<SceneObject *> Scene::GetSceneObjects() const {
  return _sceneObjects;
}

drawing::SceneDrawer * Scene::GetDrawer() const {
  return _drawer;
}

physics::ScenePhysics * Scene::GetPhysics() const {
  return _physics;
}

input::SceneInputManager * Scene::GetInput() const {
  return _inputManager;
}

void Scene::Update(float deltaTime) {
  if(_physics != nullptr) {
    _physics->FixedUpdate(0.2f);
  }
  for(const auto obj : _sceneObjects) {
    obj->Update(deltaTime);
  }
}

physics::ScenePhysics * Scene::CreatePhysics() {
  return newObject<physics::RP3DScenePhysics>();
}

drawing::SceneDrawer * Scene::CreateDrawer() {
  return newObject<drawing::SceneDrawer>();
}

input::SceneInputManager * Scene::CreateInputManager() {
  return newObject<input::SceneInputManager>();
}

SceneObject * Scene::CreateDefaultViewTarget() {
  return newObject<DefaultCamera>();
}

SceneObject * Scene::GetViewTarget() const {
  if(_viewTarget != nullptr) {
    return _viewTarget;
  }

  return _defaultViewTarget;
}

SceneObject * Scene::InitSceneObject(SceneObject * object) {
  if (HasBeenInitialized()) {
    _sceneObjects.Push(object);
    object->Init(this);
  } else {
    _objectsPendingInit.Push(object);
  }
  return object;
}
}
