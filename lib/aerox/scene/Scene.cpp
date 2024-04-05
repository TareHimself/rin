#include "aerox/drawing/scene/SceneDeferredDrawer.hpp"

#include <aerox/scene/Scene.hpp>
#include <aerox/scene/objects/DefaultCamera.hpp>
#include <aerox/Engine.hpp>
#include <aerox/input/InputSubsystem.hpp>
#include <aerox/physics/rp3/RP3DScenePhysics.hpp>
#include <aerox/drawing/scene/SceneDrawer.hpp>
#include <aerox/scene/objects/SceneObject.hpp>

namespace aerox::scene {

Engine *Scene::GetEngine() const {
  return GetOwner();
}

void Scene::OnInit(Engine * outer) {
  TOwnedBy::OnInit(outer);
  _inputConsumer = CreateInputManager();
  _physics = CreatePhysics();
  _physics->Init(this);
  _drawer = CreateDrawer();
  _drawer->Init(this);
  
  _defaultViewTarget = InitSceneObject(CreateDefaultViewTarget());
  
  if(!_objectsPendingInit.empty()) {
    for(const auto &obj : _objectsPendingInit) {
      InitSceneObject(obj);
    }
    
    _objectsPendingInit.clear();
  }
}

void Scene::OnDestroy() {
    TObjectWithInit::OnDestroy();
  _sceneObjects.clear();

  _drawer.reset();
  
  _physics.reset();

  _inputConsumer.reset();
}

void Scene::ReadFrom(Buffer &store) {
  uint64_t numObjects;
  store >> numObjects;

  for(auto i = 0; i < numObjects; i++) {
    String factoryId;
    uint64_t dataSize;
    store >> factoryId;
    store >> dataSize;
    log::engine->Warn("Re-Creation of object not yet implemented",factoryId);
    store.Skip(dataSize);
    // if(HasObjectInFactory(factoryId)) {
    //   const auto createdObject = CreateObjectFromFactory<SceneObject>(factoryId);
    //   std::vector<char> objectData;
    //   objectData.resize(dataSize);
    //   store.Read(objectData.data(),dataSize);
    //   auto dataBuffer = MemoryBuffer(objectData);
    //   createdObject->ReadFrom(dataBuffer);
    //   InitSceneObject(createdObject);
    // }
    // else {
    //   
    // }
  }
}

void Scene::WriteTo(Buffer &store) {
  uint64_t numObjects = 0;
  MemoryBuffer objectsData;
  for(const auto &sceneObject : _sceneObjects) {
    if(!ShouldSerializeObject(sceneObject)) {
      continue;
    }

    if(const auto reflectedData = sceneObject->GetMeta()) {
      // Serialize Object [id,size,data]
      objectsData << reflectedData->GetName();
      MemoryBuffer objectData;
      sceneObject->WriteTo(objectData);
      objectsData << objectData.size();
      objectsData << objectData;

      numObjects++;
    }
    
    
  }
  store << numObjects;
  store << objectsData;
}

bool Scene::ShouldSerializeObject(const std::shared_ptr<SceneObject> &object) {
  return object && object != _defaultViewTarget;
}

Array<std::weak_ptr<SceneObject>> Scene::GetSceneObjects() const {
  Array<std::weak_ptr<SceneObject>> result;
  for(auto &obj : _sceneObjects) {
    result.push(obj);
  }
  return result;
}

std::list<std::weak_ptr<LightComponent>> Scene::GetSceneLights() const {
  return _lights;
}

std::weak_ptr<drawing::SceneDrawer> Scene::GetDrawer() const {
  return _drawer;
}

std::weak_ptr<physics::ScenePhysics> Scene::GetPhysics() const {
  return _physics;
}

std::weak_ptr<input::SceneInputConsumer> Scene::GetInput() const {
  return _inputConsumer;
}

void Scene::RegisterLight(const std::weak_ptr<LightComponent> &light) {
  _lights.push_back(light);
}

void Scene::Tick(float deltaTime) {
  if(_physics) {
    _physics->FixedUpdate(0.2f);
  }
  for(const auto &obj : _sceneObjects) {
    obj->Tick(deltaTime);
  }
}

std::shared_ptr<physics::ScenePhysics> Scene::CreatePhysics() {
  return newObject<physics::RP3DScenePhysics>();
}

std::shared_ptr<drawing::SceneDrawer> Scene::CreateDrawer() {
  return newObject<drawing::SceneDeferredDrawer>();
}

std::shared_ptr<input::SceneInputConsumer> Scene::CreateInputManager() {
  return GetEngine()->GetInputSubsystem().lock()->Consume<input::SceneInputConsumer>().lock();
}

std::shared_ptr<SceneObject> Scene::CreateDefaultViewTarget() {
  return newObject<DefaultCamera>();
}

std::weak_ptr<SceneObject> Scene::GetViewTarget() const {
  if(_viewTarget) {
    return _viewTarget;
  }

  return _defaultViewTarget;
}

std::shared_ptr<SceneObject> Scene::InitSceneObject(const std::shared_ptr<SceneObject> &object) {
  if (IsInitialized() || IsInitializing()) {
      _sceneObjects.push(object);
      object->Init(this);
  } else {
    _objectsPendingInit.push(object);
  }
  return object;
}
}
