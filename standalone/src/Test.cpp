#include "Test.hpp"
#include "LightArray.hpp"
#include "TestWidget.hpp"
#include "aerox/assets/AssetSubsystem.hpp"
#include "aerox/drawing/MaterialBuilder.hpp"
#include "aerox/drawing/scene/SceneDrawer.hpp"
#include "aerox/io/io.hpp"
#include "aerox/scene/components/PointLightComponent.hpp"
#include "aerox/scene/objects/PointLight.hpp"
#include "aerox/widgets/Column.hpp"
#include "aerox/widgets/Panel.hpp"
#include "aerox/widgets/Image.hpp"
#include "aerox/widgets/Row.hpp"
#include "aerox/widgets/Sizer.hpp"
#include "aerox/widgets/Text.hpp"
#include "aerox/widgets/WidgetSubsystem.hpp"
#include "aerox/widgets/utils.hpp"
#include <aerox/scripting/ScriptSubsystem.hpp>

void PrettyShader::OnInit(widgets::WidgetSubsystem *outer) {
  Canvas::OnInit(outer);
    SetSize({512, 512});
    auto drawer = Engine::Get()->GetDrawingSubsystem().lock();

    auto shaderManager = drawer->GetShaderManager().lock();

    _material = GetOwner()->CreateMaterialInstance({drawing::Shader::FromSource(
            io::getRawShaderPath("2d/rect.vert")),
                                                                  drawing::Shader::FromSource(
                                                                          io::getRawShaderPath("2d/pretty.frag"))});
}



void PrettyShader::OnPaint(widgets::WidgetFrameData *frameData,
                           const widgets::Rect &clip) {
    const auto rect = GetDrawRect();

    if (!_material) {
        return;
    }

    widgets::WidgetPushConstants drawData{};
    drawData.clip = clip;
    drawData.extent = rect;
    //drawData.transform = glm::rotate(glm::mat4{1.0f},45.0f,glm::vec3{0.0f,0.0,1.0f});

    widgets::bindMaterial(frameData, _material);

    _material->Push(frameData->GetCmd(), "pRect", drawData);

    frameData->DrawQuad();
}



void TestGameObject::OnInit(scene::Scene * scene) {
    SceneObject::OnInit(scene);

    const auto assetImporter = GetScene()->GetEngine()->GetAssetSubsystem().lock();

    // auto font = assetImporter->ImportFont(R"(D:\Github\vengine\fonts\noto)");

    asyncTask = std::async(std::launch::async,[this, assetImporter]{
        const auto saveName = R"(D:\test.va)";
        if (const auto importedMesh = assetImporter->ImportMesh(R"(D:\test.glb)")) {
            //assetImporter->SaveAsset(saveName, importedMesh);
            _mesh.reset();
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

            const auto defaultMat = GetScene()->GetDrawer().lock()->GetDefaultMaterial().lock();
            defaultMat->SetTexture("ColorT", color);
            defaultMat->SetTexture("NormalT", normal);
            defaultMat->SetTexture("RoughnessT", roughness);
        }
        _meshComponent.lock()->SetMesh(_mesh);
    });


    if (const auto lightArray = GetScene()->CreateSceneObject<LightArray>().lock()) {
        log::engine->Info("Light Array Created");
    }

    SetWorldLocation({0.0f, 0.0f, 0.0f});
    SetWorldRotation(GetWorldRotation().ApplyYaw(180.0f));
    AddCleanup([this] {
        _mesh.reset();
    });

    // _fpsWidget = GetEngine()->GetWidgetManager().lock()->AddWidget<widgets::Text>();
    // _fpsWidget.lock()->SetFontSize(20);

    GetInput().lock()->BindKey(window::Key_9,[](const std::shared_ptr<input::KeyInputEvent> &e){
      const auto script = Engine::Get()->GetScriptSubsystem().lock()->ScriptFromFile(R"(D:\Github\vengine\scripts\test.vs)");
        script->Run();
        return false;
    },{});

    GetInput().lock()->BindKey(window::Key_1,
                                  [this](
                                          const std::shared_ptr<input::KeyInputEvent> &e) {
                                    const auto widgetManager = GetEngine()->
                                                               GetWidgetSubsystem().lock();

                                      if (const auto root = widgetManager->GetRoot(
                                              e->GetWindow()).lock()) {
                                        const auto testWidget = widgetManager->
                                                  CreateWidget<TestWidget>();
                                          root->Add(testWidget);
                                          testWidget->BindInput(e->GetWindow());

                                      }
                                      return true;
                                  }, {});

}


void TestGameObject::AttachComponentsToRoot(
        const std::weak_ptr<scene::SceneComponent> &root) {
    SceneObject::AttachComponentsToRoot(root);
    _meshComponent.lock()->AttachTo(root);
}

void TestGameObject::Tick(float deltaTime) {
    SceneObject::Tick(deltaTime);
    //_fpsWidget.lock()->SetContent("FPS :" + std::to_string(
    //                                     static_cast<int>(round(1.0f / deltaTime))));
    //SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime));
    // Rotate Object
}

math::Transform TestGameObject::GetWorldTransform() const {
    return SceneObject::GetWorldTransform();
}


