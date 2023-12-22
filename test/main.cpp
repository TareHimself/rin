

#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/physics/rp3/RP3DScenePhysics.hpp>

int main(int argc, char** argv){
    

    try {
      const auto engine = new vengine::Engine();
      engine->setApplicationName("Test Application");
      engine->addScene(new vengine::scene::Scene());
      engine->run();
    } catch (const std::exception& e) {
      std::cerr << e.what() << std::endl;
      return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}