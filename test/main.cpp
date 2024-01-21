
#include "vengine/assets/AssetManager.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/io/io.hpp"
#include "vengine/drawing/Mesh.hpp"
#include "vengine/drawing/Texture.hpp"
#include "vengine/math/constants.hpp"
#include "vengine/scene/components/ScriptComponent.hpp"
#include "vengine/scene/components/StaticMeshComponent.hpp"
#include "vengine/scripting/Script.hpp"

#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
#include <vengine/math/Vector.hpp>
using namespace vengine;


class TestMesh : public drawing::Mesh {
public:
  TestMesh();
};
class TestGameObject : public scene::SceneObject {

  drawing::Mesh * mesh = newObject<TestMesh>();

  scene::StaticMeshComponent * meshComponent = AddComponent<scene::StaticMeshComponent>();
  scene::ScriptComponent * scriptComp = AddComponent<scene::ScriptComponent>(R"(D:\Github\vengine\scripts\test.as)");
  void Init(scene::Scene *outer) override;

  void AttachComponentsToRoot(scene::SceneComponent *root) override;

  void Update(float deltaTime) override;
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

void TestGameObject::Init(scene::Scene *outer) {
  SceneObject::Init(outer);
  mesh->Init(GetEngine()->GetDrawer());
  const auto assetImporter = GetOuter()->GetEngine()->GetAssetManager();
  const auto saveName = R"(D:\test.va)";
  const auto importedMesh = assetImporter->ImportMesh(R"(D:\test.glb)");
  if(importedMesh != nullptr) {
    assetImporter->SaveAsset(saveName,importedMesh);
    mesh->Destroy();
    mesh = importedMesh;
  }
  const auto color = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_COL_2K_METALNESS.png)");
  const auto normal = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_NRM_2K_METALNESS.png)");
  const auto roughness = assetImporter->ImportTexture(R"(D:\MetalGoldPaint002\MetalGoldPaint002_ROUGHNESS_2K_METALNESS.png)");
  if(color) {
    log::assets->info("Loaded Texture");
    const auto defaultMat = GetEngine()->GetDrawer()->GetDefaultCheckeredMaterial();
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
  
  meshComponent->SetMesh(mesh);

  SetWorldLocation({0,0,0});
  AddCleanup([=] {
    meshComponent->SetMesh(nullptr);
    mesh->Destroy();
    mesh = nullptr;
  });

  
}

void TestGameObject::AttachComponentsToRoot(scene::SceneComponent *root) {
  SceneObject::AttachComponentsToRoot(root);
  meshComponent->AttachTo(root);
}

void TestGameObject::Update(float deltaTime) {
  SceneObject::Update(deltaTime);

  SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime)); // Rotate Object
}

int main(int argc, char** argv){
    

    try {
      // auto filePath = R"(D:\testData.v)";
      // if(false) {
      //   auto test = OutFileBuffer(filePath);
      //   test << 0;
      //   test << 1;
      //   test << 2;
      //   test << 3;
      //   test.close();
      // } else {
      //   
      //   auto test = InFileBuffer(filePath);
      //   int data = -1;
      //   test >> data;
      //   std::cout << data << std::endl;
      //   test >> data;
      //   test >> data;
      //   test >> data;
      //   test.close();
      // }
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
