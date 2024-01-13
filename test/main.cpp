

#include "vengine/assets/AssetManager.hpp"
#include "vengine/io/io.hpp"
#include "vengine/drawing/Mesh.hpp"
#include "vengine/scene/components/StaticMeshComponent.hpp"

#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
using namespace vengine;


class TestMesh : public drawing::Mesh {
public:
  TestMesh();
};
class TestGameObject : public scene::SceneObject {

  drawing::Mesh * mesh = newObject<TestMesh>();

  scene::StaticMeshComponent * meshComponent = createRenderedComponent<scene::StaticMeshComponent>();
  
  void init(scene::Scene *outer) override;

  void attachComponentsToRoot(scene::SceneComponent *root) override;

  void update(float deltaTime) override;
};

TestMesh::TestMesh() {
  _asset.vertices.resize(4);
  _asset.indices.resize(6);
  
  _asset.vertices[0].location = {0.5,-0.5, 0};
  _asset.vertices[1].location = {0.5,0.5, 0};
  _asset.vertices[2].location = {-0.5,-0.5, 0};
  _asset.vertices[3].location = {-0.5,0.5, 0};

  _asset.vertices[0].color = {0,0, 0,1};
  _asset.vertices[1].color = { 0.5,0.5,0.5 ,1};
  _asset.vertices[2].color = { 1,0, 0,1 };
  _asset.vertices[3].color = { 0,1, 0,1 };
  
  _asset.indices[0] = 0;
  _asset.indices[1] = 1;
  _asset.indices[2] = 2;

  _asset.indices[3] = 2;
  _asset.indices[4] = 1;
  _asset.indices[5] = 3;
  assets::MeshSurface surface = {0,6};
  _asset.surfaces.push(surface);
}

void TestGameObject::init(scene::Scene *outer) {
  SceneObject::init(outer);
  const auto meshAsset = getOuter()->getEngine()->getAssetManager()->importMesh(R"(D:\test.glb)");
  mesh->setAsset(meshAsset.value());
  mesh->init(getOuter()->getOuter());
  
  mesh->upload();
  
  meshComponent->setMesh(mesh);

  getRootComponet()->setRelativeLocation({0,0,0});

  addCleanup([=] {
    meshComponent->setMesh(nullptr);
    mesh->cleanup();
    mesh = nullptr;
  });

  
}

void TestGameObject::attachComponentsToRoot(scene::SceneComponent *root) {
  SceneObject::attachComponentsToRoot(root);
  meshComponent->attachTo(root);
}

void TestGameObject::update(float deltaTime) {
  SceneObject::update(deltaTime);
  getRootComponet()->setRelativeLocation({0,sin(getOuter()->getEngine()->getEngineTimeSeconds()) * 3.f,0});
  log::engine->info("Moving Mesh");
}

int main(int argc, char** argv){
    

    try {
      io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
      auto engine = vengine::newObject<Engine>();
      auto scene = newObject<scene::Scene>();
      engine->setAppName("Test Application");
      engine->addScene(scene);
      
      auto triangleObj = scene->createSceneObject<TestGameObject>();
      engine->run();
      triangleObj = nullptr;
      scene = nullptr;
      engine = nullptr;
    } catch (const std::exception& e) {
      std::cerr << e.what() << std::endl;
      return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}
