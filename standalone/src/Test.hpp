#pragma once
#include "aerox/drawing/Mesh.hpp"
#include "aerox/scene/components/ScriptComponent.hpp"
#include "aerox/scene/components/StaticMeshComponent.hpp"
#include "aerox/scene/objects/SceneObject.hpp"
#include "aerox/widgets/Canvas.hpp"
#include "Test.gen.hpp"

using namespace aerox;

class PrettyShader : public widgets::Canvas {
  std::shared_ptr<drawing::MaterialInstance> _material;
public:
  void OnInit(widgets::WidgetSubsystem * outer) override;
  void OnPaint(widgets::WidgetFrameData *frameData, const widgets::Rect& clip) override;
  
};


META_TYPE()
class TestGameObject : public scene::SceneObject {
public:

    META_BODY()

    std::future<void> asyncTask;
protected:
  META_PROPERTY()
  std::shared_ptr<drawing::Mesh> _mesh;

    META_PROPERTY()
  std::weak_ptr<scene::StaticMeshComponent> _meshComponent = AddComponent<
    scene::StaticMeshComponent>();

    META_PROPERTY()
  std::weak_ptr<scene::ScriptComponent> _scriptComp = AddComponent<
    scene::ScriptComponent>(R"(D:\Github\vengine\scripts\test.vs)");

public:

  void OnInit(scene::Scene * scene) override;

  void AttachComponentsToRoot(const std::weak_ptr<scene::SceneComponent> &root) override;

  void Tick(float deltaTime) override;

    META_FUNCTION()
  math::Transform GetWorldTransform() const override;

    META_FUNCTION()
  static std::shared_ptr<TestGameObject> Construct() {
    return newObject<TestGameObject>();
  }
};
