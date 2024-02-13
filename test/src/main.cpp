// #define VMA_DEBUG_LOG(format, ...) do { \
// printf(format, __VA_ARGS__); \
// printf("\n"); \
// } while(false)
#include "Test.hpp"
#include "vengine/io/io.hpp"
#include "vengine/scene/objects/PointLight.hpp"
#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
using namespace vengine;

int main(int argc, char **argv) {

  try {

    io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
    const auto engine = new Engine();
    engine->SetAppName("Test Application");
    const auto scene = engine->CreateScene<scene::Scene>();
    auto triangleObj = scene.Reserve()->CreateSceneObject<TestGameObject>();
    
    auto values =  reflect::factory::values();
    if(const auto reflectedObj = reflect::factory::find<TestGameObject>()) {
      
      if(const auto func = reflectedObj->GetFunction("GetWorldTransform")) {
        math::Transform objectTransform{{},{},{20.f,20.f,20.f}};
        func->Call(&objectTransform,triangleObj.Reserve().Get());
      }
    }
    engine->Run();
  } catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
    return EXIT_FAILURE;
  }

  return EXIT_SUCCESS;
}
//
//