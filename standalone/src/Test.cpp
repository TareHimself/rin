#include "Test.hpp"
#include "LightArray.hpp"
#include "TestWidget.hpp"
#include "vengine/assets/AssetSubsystem.hpp"
#include "vengine/containers/TAsyncTask.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/io/io.hpp"
#include "vengine/scene/components/PointLightComponent.hpp"
#include "vengine/scene/objects/PointLight.hpp"
#include "vengine/widget/Column.hpp"
#include "vengine/widget/Panel.hpp"
#include "vengine/widget/Image.hpp"
#include "vengine/widget/Row.hpp"
#include "vengine/widget/Sizer.hpp"
#include "vengine/widget/Text.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"
#include "vengine/widget/utils.hpp"


void PrettyShader::Init(widget::WidgetSubsystem *outer) {
  Canvas::Init(outer);
  SetSize({250, 250});
  auto drawer = outer->GetOuter()->GetDrawingSubsystem().Reserve();

  auto shaderManager = drawer->GetShaderManager().Reserve();

  _material = drawing::MaterialBuilder()
              .SetType(drawing::EMaterialType::UI)
              .AddShader(drawing::Shader::FromSource(
                  io::getRawShaderPath("2d/rect.vert")))
              .AddShader(drawing::Shader::FromSource(
                  io::getRawShaderPath("2d/pretty.frag")))
              .Create();
}

void PrettyShader::OnPaint(widget::WidgetFrameData *frameData,
                           const widget::Rect &clip) {
  const auto rect = GetDrawRect();

  if (!_material) {
    return;
  }

  widget::WidgetPushConstants drawData{};
  drawData.clip = clip;
  drawData.extent = rect;

  widget::bindMaterial(frameData, _material);

  _material->Push(frameData->GetCmd(), "pRect", drawData);

  frameData->DrawQuad();
}

void TestGameObject::Init(scene::Scene *outer) {
  SceneObject::Init(outer);

  const auto assetImporter = GetOuter()->GetEngine()->GetAssetSubsystem().
                                         Reserve();

  auto font = assetImporter->ImportFont(R"(D:\Github\vengine\fonts\noto)");

  if (auto loadTask = AsyncTaskManager::Get()->CreateTask<void *>(
      [this,assetImporter] {

        const auto saveName = R"(D:\test.va)";
        const auto importedMesh = assetImporter->ImportMesh(R"(D:\test.glb)");
        if (importedMesh != nullptr) {
          //assetImporter->SaveAsset(saveName, importedMesh);
          _mesh.Clear();
          _mesh = importedMesh;
        }

        const auto color = assetImporter->ImportTexture(
            R"(D:\MetalGoldPaint002\MetalGoldPaint002_COL_2K_METALNESS.png)");
        const auto normal = assetImporter->ImportTexture(
            R"(D:\MetalGoldPaint002\MetalGoldPaint002_NRM_2K_METALNESS.png)");
        const auto roughness = assetImporter->ImportTexture(
            R"(D:\MetalGoldPaint002\MetalGoldPaint002_ROUGHNESS_2K_METALNESS.png)");
        const auto pfp = assetImporter->ImportTexture(R"(D:\dog.png)");
        if (color) {
          const auto defaultMat = GetScene()->GetDrawer().Reserve()->
                                              GetDefaultMaterial().Reserve();
          defaultMat->SetTexture("ColorT", color);
          defaultMat->SetTexture("NormalT", normal);
          defaultMat->SetTexture("RoughnessT", roughness);
        }
        // const auto existingMesh = assetImporter->LoadMeshAsset(saveName);
        // if(existingMesh != nullptr) {
        //   mesh->Destroy();
        //   mesh = existingMesh;
        // } else {
        //   const auto importedMesh = assetImporter->ImportMesh(R"(D:\test.glb)");
        //   if(importedMesh != nullptr) {
        //     assetImporter->SaveAsset(saveName,importedMesh);
        //     mesh->Destroy();
        //     mesh = importedMesh;
        //   }
        // }

        _meshComponent.Reserve()->SetMesh(_mesh);
        return nullptr;
      })) {
    loadTask->onFinish.Bind([](void *) {
      log::engine->info("Async load of data complete");
    });
    loadTask->Run();
  }

  if (auto lightArray = GetScene()->CreateSceneObject<LightArray>().Reserve()) {
    log::engine->info("Light Array Created");
  }

  SetWorldLocation({0.0f, 0.0f, 0.0f});
  SetWorldRotation(GetWorldRotation().ApplyYaw(180.0f));
  AddCleanup([this] {
    _meshComponent.Reserve()->SetMesh(nullptr);
    _mesh.Clear();
  });

  // _fpsWidget = GetEngine()->GetWidgetManager().Reserve()->AddWidget<widget::Text>();
  // _fpsWidget.Reserve()->SetFontSize(20);

  GetInput().Reserve()->BindKey(window::Key_1,
                                [this](
                                const std::shared_ptr<input::KeyInputEvent> & e) {
                                  auto widgetManager = GetEngine()->
                                      GetWidgetSubsystem().Reserve();

                                  if (auto root = widgetManager->GetRoot(
                                      e->GetWindow()).Reserve()) {
                                    auto testWidget =widgetManager->
                                        CreateWidget<TestWidget>();
                                    root->Add(testWidget);
                                    testWidget->BindInput(e->GetWindow());

                                  }
                                  return true;
                                }, {});
}

void TestGameObject::AttachComponentsToRoot(
    const Ref<scene::SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _meshComponent.Reserve()->AttachTo(root);
}

void TestGameObject::Tick(float deltaTime) {
  SceneObject::Tick(deltaTime);
  //_fpsWidget.Reserve()->SetContent("FPS :" + std::to_string(
  //                                     static_cast<int>(round(1.0f / deltaTime))));
  //SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime));
  // Rotate Object
}

math::Transform TestGameObject::GetWorldTransform() const {
  return SceneObject::GetWorldTransform();
}
