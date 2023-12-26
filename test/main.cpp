

#include "vengine/io/io.hpp"

#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>
using namespace vengine;
int main(int argc, char** argv){
    

    try {
      auto engine = vengine::newObject<Engine>();
      engine->setApplicationName("Test Application");
      engine->addScene(newObject<scene::Scene>());
      io::setRawShadersPath(R"(D:\Github\vengine\shaders)");
      
      engine->run();
      engine = nullptr;
    } catch (const std::exception& e) {
      std::cerr << e.what() << std::endl;
      return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}