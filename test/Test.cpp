#include "Test.hpp"

#include "vengine/assets/AssetManager.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/widget/Image.hpp"


TestMesh::TestMesh() {
  // auto vertices = Array<drawing::Vertex>();
  // auto indices = Array<uint32_t>();
  // vertices.resize(4);
  // indices.resize(6);
  //
  // vertices[0].location = {0.5,-0.5, 0,0};
  // vertices[1].location = {0.5,0.5, 0,0};
  // vertices[2].location = {-0.5,-0.5, 0,0};
  // vertices[3].location = {-0.5,0.5, 0,0};
  //
  // indices[0] = 0;
  // indices[1] = 1;
  // indices[2] = 2;
  //
  // indices[3] = 2;
  // indices[4] = 1;
  // indices[5] = 3;
  // SetVertices(vertices);
  // SetIndices(indices);
  // SetSurfaces({{0,6}});
}




void TestGameObject::Init(scene::Scene * outer) {
  SceneObject::Init(outer);
  _mesh->Init(GetEngine()->GetDrawer().Reserve().Get());
  const auto assetImporter = GetOuter()->GetEngine()->
                                        GetAssetManager().Reserve();
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

  SetWorldLocation({0, 0, 0});
  AddCleanup([=] {
    _meshComponent.Reserve()->SetMesh(nullptr);
    _mesh.Clear();
  });

  GetInput().Reserve()->BindKey(SDLK_f, [=](input::KeyInputEvent) {
                               const auto image = GetEngine()->GetWidgetManager().Reserve()->AddWidget<widget::Image>().Reserve();
                               // const auto text = GetEngine()->GetWidgetManager().Reserve()->
                               //     AddWidget<widget::Text>().Reserve();

                               image->SetTexture(pfp);
                               image->SetRect({{0, 0}, {512, 512}});

                               // text->SetContent(
                               //     "Oritsemisan   Metseagharun   is   GAY ?????");
                               return true;
                             }, [=](input::KeyInputEvent) {

                               return false;
                             });
}

void TestGameObject::AttachComponentsToRoot(const WeakPointer<scene::SceneComponent> &root) {
  SceneObject::AttachComponentsToRoot(root);
  _meshComponent.Reserve()->AttachTo(root);
}

void TestGameObject::Update(float deltaTime) {
  SceneObject::Update(deltaTime);

  SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime));
  // Rotate Object
}

math::Transform TestGameObject::GetWorldTransform() const {
  return SceneObject::GetWorldTransform();
}
