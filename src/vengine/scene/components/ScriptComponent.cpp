#include <vengine/scene/components/ScriptComponent.hpp>
#include "vengine/scene/objects/SceneObject.hpp"
#include <vengine/scripting/ScriptManager.hpp>
namespace vengine::scene {

ScriptComponent::ScriptComponent() {
}

void ScriptComponent::Init(SceneObject * outer) {
  SceneComponent::Init(outer);
  _script = GetOuter()->GetEngine()->GetScriptManager().Reserve()->ScriptFromFile(_scriptPath);
  
  if(!_script) {
    return;
  }

  _script->InitScript();
}

ScriptComponent::ScriptComponent(const std::filesystem::path &path) {
  _scriptPath = path;
}
}
