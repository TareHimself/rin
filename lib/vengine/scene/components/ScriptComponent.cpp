#include <vengine/scene/components/ScriptComponent.hpp>
#include "vengine/scene/objects/SceneObject.hpp"
#include <vengine/scripting/ScriptSubsystem.hpp>
namespace vengine::scene {

ScriptComponent::ScriptComponent() {
}

void ScriptComponent::Init(SceneObject * outer) {
  SceneComponent::Init(outer);
  _script = GetOuter()->GetEngine()->GetScriptSubsystem().Reserve()->ScriptFromFile(_scriptPath);
  
  if(!_script) {
    return;
  }

  _script->InitScript();
}

ScriptComponent::ScriptComponent(const fs::path &path) {
  _scriptPath = path;
}
}
