// #define VMA_DEBUG_LOG(format, ...) do { \
// printf(format, __VA_ARGS__); \
// printf("\n"); \
// } while(false)
#include "Test.hpp"
#include "aerox/io/IoSubsystem.hpp"
#include "aerox/io/io.hpp"
#include "aerox/scene/objects/PointLight.hpp"
#include <cstdlib>
#include <iostream>
#include <ostream>
#include <aerox/Engine.hpp>
#include <aerox/scene/Scene.hpp>
#include <aerox/physics/rp3/RP3DScenePhysics.hpp>
using namespace aerox;

int main(int argc, char **argv) {

  try {

    
    io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
    Engine::Get()->SetAppName("Test Application");
    
    const auto scene = Engine::Get()->CreateScene<scene::Scene>();
    auto t1 = scene.lock()->CreateSceneObject<TestGameObject>();

    auto j = scene.lock()->ToJson();

    scene.lock()->FromJson(j);

    std::ofstream o(R"(D:\Github\vengine\test.json)");
    o << std::setw(4) << j << std::endl;
    o.close();
    
    Engine::Get()->Run();
  } catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
    return EXIT_FAILURE;
  }

  return EXIT_SUCCESS;
}
//
//