#include "vengine/assets/AssetManager.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/io/io.hpp"
#include "vengine/drawing/Mesh.hpp"
#include "vengine/drawing/Texture.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/math/constants.hpp"
#include "vengine/scene/SceneObject.hpp"
#include "vengine/scene/components/ScriptComponent.hpp"
#include "vengine/scene/components/StaticMeshComponent.hpp"
#include "vengine/widget/WidgetManager.hpp"
#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
#include <vengine/widget/Widget.hpp>
using namespace vengine;


class TestMesh : public drawing::Mesh {
public:
  TestMesh();
};

TestMesh::TestMesh() {
  auto vertices = Array<drawing::Vertex>();
  auto indices = Array<uint32_t>();
  vertices.resize(4);
  indices.resize(6);
  
  vertices[0].location = {0.5,-0.5, 0,0};
  vertices[1].location = {0.5,0.5, 0,0};
  vertices[2].location = {-0.5,-0.5, 0,0};
  vertices[3].location = {-0.5,0.5, 0,0};
  
  indices[0] = 0;
  indices[1] = 1;
  indices[2] = 2;

  indices[3] = 2;
  indices[4] = 1;
  indices[5] = 3;
  SetVertices(vertices);
  SetIndices(indices);
  SetSurfaces({{0,6}});
}

class TestGameObject : public scene::SceneObject {

  drawing::Mesh * _mesh = newObject<TestMesh>();

  scene::StaticMeshComponent * _meshComponent = AddComponent<scene::StaticMeshComponent>();
  scene::ScriptComponent * _scriptComp = AddComponent<scene::ScriptComponent>(R"(D:\Github\vengine\scripts\test.as)");
  void Init(scene::Scene *outer) override;

  void AttachComponentsToRoot(scene::SceneComponent *root) override;

  void Update(float deltaTime) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(TestGameObject)
};


void TestGameObject::Init(scene::Scene *outer) {
  SceneObject::Init(outer);
  
  _mesh->Init(GetEngine()->GetDrawer());
  const auto assetImporter = GetOuter()->GetEngine()->GetAssetManager();
  const auto saveName = R"(D:\test.va)";
  const auto importedMesh = assetImporter->ImportMesh(R"(D:\test.glb)");
  if(importedMesh != nullptr) {
    assetImporter->SaveAsset(saveName,importedMesh);
    _mesh->Destroy();
    _mesh = importedMesh;
  }
  const auto color = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_COL_2K_METALNESS.png)");
  const auto normal = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_NRM_2K_METALNESS.png)");
  const auto roughness = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_ROUGHNESS_2K_METALNESS.png)");
  AddCleanup([&] {
    color->Destroy();
    normal->Destroy();
    roughness->Destroy();
  });
  
  if(color) {
    const auto defaultMat = GetScene()->GetDrawer()->GetDefaultMaterial();
    defaultMat->SetTexture("ColorT",color);
    defaultMat->SetTexture("NormalT",normal);
    defaultMat->SetTexture("RoughnessT",roughness);
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
  
  _meshComponent->SetMesh(_mesh);

  SetWorldLocation({0,0,0});
  AddCleanup([=] {
    _meshComponent->SetMesh(nullptr);
    _mesh->Destroy();
    _mesh = nullptr;
  });

  GetEngine()->GetWidgetManager()->AddWidget<widget::Widget>();
}

void TestGameObject::AttachComponentsToRoot(scene::SceneComponent *root) {
  SceneObject::AttachComponentsToRoot(root);
  _meshComponent->AttachTo(root);
}

void TestGameObject::Update(float deltaTime) {
  SceneObject::Update(deltaTime);

  SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime)); // Rotate Object
}

int main(int argc, char** argv){
    

    try {

      
      io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
      auto engine = vengine::newObject<Engine>();
      engine->SetAppName("Test Application");
      auto scene = engine->CreateScene<scene::Scene>();
      auto triangleObj = scene->CreateSceneObject<TestGameObject>();
      
      engine->Run();
      triangleObj = nullptr;
      scene = nullptr;
      engine = nullptr;
    } catch (const std::exception& e) {
      std::cerr << e.what() << std::endl;
      return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}
