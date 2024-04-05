#include <aerox/scene/components/ScriptComponent.hpp>
#include "aerox/scene/objects/SceneObject.hpp"
#include <aerox/scripting/ScriptSubsystem.hpp>
namespace aerox::scene {

ScriptComponent::ScriptComponent() = default;

void ScriptComponent::OnInit(SceneObject * owner) {
  SceneComponent::OnInit(owner);
  _script = Engine::Get()->GetScriptSubsystem().lock()->ScriptFromFile(_scriptPath);
  
  if(!_script) {
    return;
  }

    _script->Run();
}

ScriptComponent::ScriptComponent(const fs::path &path) {
  _scriptPath = path;
}
}
