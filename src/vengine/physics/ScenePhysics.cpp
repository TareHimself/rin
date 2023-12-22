#include "ScenePhysics.hpp"


namespace vengine {
namespace physics {
scene::Scene * ScenePhysics::getWorld()
{
return _world;
}

void ScenePhysics::setWorld(scene::Scene * newWorld)
{
    _world = newWorld;
}

void ScenePhysics::fixedUpdate(float deltaTime) {}
}
}