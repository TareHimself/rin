#include "Test.hpp"
#include "LightArray.hpp"
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
#include "vengine/widget/WidgetSubsystem.hpp"


void PrettyShader::Init(widget::WidgetSubsystem *outer) {
  Canvas::Init(outer);
  SetSize({250, 250});
  auto drawer = outer->GetOuter()->GetDrawingSubsystem().Reserve();

  auto shaderManager = drawer->GetShaderManager().Reserve();

  _material = drawing::MaterialBuilder()
  .SetType(drawing::EMaterialType::UI)
  .ConfigurePushConstant<widget::WidgetPushConstants>("pRect")
  .AddShader(drawing::Shader::FromSource(shaderManager.Get(),io::getRawShaderPath("2d/rect.vert")))
  .AddShader(drawing::Shader::FromSource(shaderManager.Get(),io::getRawShaderPath("2d/pretty.frag")))
  .Create(drawer.Get());

  _material->SetBuffer<widget::UiGlobalBuffer>("UiGlobalBuffer",outer->GetGlobalBuffer());
}

void PrettyShader::OnPaint(drawing::SimpleFrameData *frameData,
                           widget::Rect rect) {
  if (!_material) {
    return;
  }

  widget::WidgetPushConstants drawData{};

  drawData.extent = rect;
  drawData.time.x = GetOuter()->GetEngine()->GetEngineTimeSeconds() - GetTimeAtInit();

  _material->BindPipeline(frameData->GetRaw());
  _material->BindSets(frameData->GetRaw());

  _material->PushConstant(frameData->GetCmd(), "pRect", drawData);

  frameData->DrawQuad();
}

void TestGameObject::Init(scene::Scene *outer) {
  SceneObject::Init(outer);
  const auto assetImporter = GetOuter()->GetEngine()->GetAssetSubsystem().
                                         Reserve();

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

  SetWorldLocation({0, 0, 0});
  SetWorldRotation(GetWorldRotation().ApplyYaw(180.0f));
  AddCleanup([=] {
    _meshComponent.Reserve()->SetMesh(nullptr);
    _mesh.Clear();
  });

  // _fpsWidget = GetEngine()->GetWidgetManager().Reserve()->AddWidget<widget::Text>();
  // _fpsWidget.Reserve()->SetFontSize(20);

  auto widgetManager = GetEngine()->
                       GetWidgetSubsystem().Reserve();
  const auto canvas = widgetManager->CreateWidget<widget::Panel>();

  widgetManager->AddToScreen(canvas);

  auto column = widgetManager->CreateWidget<widget::Row>();

  column->SetPivot({0.5f, 0.5f});
  auto columnSlot = canvas->AddChild(column).Reserve();
  columnSlot->SetAnchorX({0.5, 0.5});
  columnSlot->SetAnchorY({0.5, 0.5});
  columnSlot->SetSizeToContent(true);

  GetInput().Reserve()->BindKey(window::EKey::Key_F,
                                [assetImporter,column,widgetManager](
                                std::shared_ptr<input::KeyInputEvent>) {

                                  // AsyncTaskManager::Get()->CreateTask<void *>(
                                  //     [assetImporter,widgetManager,column] {
                                  //       const auto background = assetImporter->
                                  //           ImportTexture(
                                  //               R"(C:\Users\Taree\Pictures\Wallpaperz\silhouette of steel ridge wallpaper, blue and pink sky painting 7b507f40-43f9-484f-9bd9-16b7443428cd.jpg)");
                                  //
                                  //       const auto sizer = widgetManager->
                                  //           CreateWidget
                                  //           <widget::Sizer>();
                                  //       const auto image = widgetManager->
                                  //           CreateWidget
                                  //           <widget::Image>();
                                  //       sizer->
                                  //           AddChild(image);
                                  //
                                  //       auto actualImage = image;
                                  //
                                  //       actualImage->SetTexture(background);
                                  //
                                  //       const auto textureDims = background->
                                  //           GetSize();
                                  //       constexpr int imageHeight = 250.0f;
                                  //       auto imageWidth =
                                  //           static_cast<float>(textureDims.
                                  //             width) /
                                  //           static_cast<float>(textureDims.
                                  //             height)
                                  //           * imageHeight;
                                  //
                                  //       // sizerSlot->SetRect(
                                  //       //     {0, 0, {imageWidth, imageHeight}});
                                  //
                                  //       sizer->SetWidth(imageWidth);
                                  //       sizer->SetHeight(imageHeight);
                                  //
                                  //       column->AddChild(sizer);
                                  //       return nullptr;
                                  //     })->Run();

                                  AsyncTaskManager::Get()->CreateTask<void *>(
                                      [assetImporter,widgetManager,column] {
                                        const auto background = assetImporter->
                                            ImportTexture(
                                                R"(C:\Users\Taree\Pictures\Wallpaperz\silhouette of steel ridge wallpaper, blue and pink sky painting 7b507f40-43f9-484f-9bd9-16b7443428cd.jpg)");

                                        const auto sizer = widgetManager->
                                            CreateWidget
                                            <widget::Sizer>();
                                        const auto shaderWidget = widgetManager
                                            ->
                                            CreateWidget
                                            <PrettyShader>();
                                        sizer->
                                            AddChild(shaderWidget);

                                        // sizer->SetWidth(imageWidth);
                                        // sizer->SetHeight(imageHeight);

                                        column->AddChild(sizer);
                                        return nullptr;
                                      })->Run();
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
