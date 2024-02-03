#pragma once
#include "vengine/drawing/Mesh.hpp"
#include "vengine/scene/components/ScriptComponent.hpp"
#include "vengine/scene/components/StaticMeshComponent.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include "reflect/Macro.hpp"
#include "Test.reflect.hpp"
#include "vengine/widget/Text.hpp"
using namespace vengine;

RCLASS()
class TestMesh : public drawing::Mesh {
public:
  TestMesh();
};


RCLASS()
class TestGameObject : public scene::SceneObject {
public:
  
  RPROPERTY()
  Ref<drawing::Mesh> _mesh = vengine::newSharedObject<TestMesh>();

  RPROPERTY()
  WeakRef<scene::StaticMeshComponent> _meshComponent = AddComponent<scene::StaticMeshComponent>();

  RPROPERTY()
  WeakRef<scene::ScriptComponent> _scriptComp = AddComponent<scene::ScriptComponent>(R"(D:\Github\vengine\scripts\test.as)");

  WeakRef<widget::Text> _fpsWidget;
  
  void Init( scene::Scene * outer) override;

  void AttachComponentsToRoot(const  WeakRef<scene::SceneComponent> &root) override;

  void Update(float deltaTime) override;

  RFUNCTION()
  math::Transform GetWorldTransform() const override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(TestGameObject)
};

REFLECT_IMPLEMENT(TestGameObject)
