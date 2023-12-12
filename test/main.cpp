

#include <cstdlib>
#include <iostream>
#include <ostream>
#include <vengine/Engine.hpp>
#include <vengine/world/World.hpp>
#include <vengine/physics/rp3/RP3PhysicsInstance.hpp>

int main(int argc, char** argv){
    

    try {
      const auto engine = new vengine::Engine();
      engine->setApplicationName("Test Application");
      engine->addWorld(new vengine::world::World());
      engine->run();
    } catch (const std::exception& e) {
      std::cerr << e.what() << std::endl;
      return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}