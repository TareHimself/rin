#pragma once
#include "vengine/drawing/Mesh.hpp"
#include "vengine/scene/components/ScriptComponent.hpp"
#include "vengine/scene/components/StaticMeshComponent.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include "reflect/Macro.hpp"
#include "Test.reflect.hpp"
#include "vengine/widget/Canvas.hpp"
// #include "vengine/widget/Text.hpp"
using namespace vengine;

class PrettyShader : public widget::Canvas {
  Managed<drawing::MaterialInstance> _material;
public:
  void Init(widget::WidgetSubsystem *outer) override;
  void OnPaint(drawing::SimpleFrameData *frameData,const widget::Rect& clip) override;
  
};


RCLASS()
class TestGameObject : public scene::SceneObject {
public:
  RPROPERTY()
  Managed<drawing::Mesh> _mesh;

  RPROPERTY()
  Ref<scene::StaticMeshComponent> _meshComponent = AddComponent<
    scene::StaticMeshComponent>();

  RPROPERTY()
  Ref<scene::ScriptComponent> _scriptComp = AddComponent<
    scene::ScriptComponent>(R"(D:\Github\vengine\scripts\test.as)");

  // Ref<widget::Text> _fpsWidget;

  void Init(scene::Scene *outer) override;

  void AttachComponentsToRoot(const Ref<scene::SceneComponent> &root) override;

  void Tick(float deltaTime) override;

  RFUNCTION()
  math::Transform GetWorldTransform() const override;

  RFUNCTION()
  static Managed<TestGameObject> Construct() {
    return newManagedObject<TestGameObject>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(TestGameObject)

};

REFLECT_IMPLEMENT(TestGameObject)
