#include "ScriptComponent.hpp"
#include "vengine/scene/SceneObject.hpp"
#include <vengine/scripting/ScriptManager.hpp>
namespace vengine::scene {

void ScriptComponent::Init(SceneObject *outer) {
  SceneComponent::Init(outer);
  _script= GetOuter()->GetEngine()->GetScriptManager()->ScriptFromFile(_scriptPath);
  if(_script == nullptr) {
    return;
  }

  _script->InitScript();
}

ScriptComponent::ScriptComponent(const std::filesystem::path &path) {
  _scriptPath = path;
}
}
