
#include "MainScene.hpp"
#include "aerox/io/IoSubsystem.hpp"
#include "aerox/io/io.hpp"
#include "aerox/scene/objects/PointLight.hpp"
#include <cstdlib>
#include <iostream>
#include <ostream>
#include <aerox/Engine.hpp>
using namespace vengine;

int main(int argc, char **argv) {

  try {

    
    io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
    Engine::Get()->SetAppName("Test Application");
    
    const auto scene = Engine::Get()->CreateScene<MainScene>();
    Engine::Get()->Run();
  } catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
    return EXIT_FAILURE;
  }

  return EXIT_SUCCESS;
}
//
//