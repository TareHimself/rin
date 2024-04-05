#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/ScriptComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> ScriptComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(ScriptComponent::Construct());
},true,{})

.Create<ScriptComponent>("ScriptComponent","",{});
}();


std::shared_ptr<meta::Metadata> ScriptComponent::GetMeta() const
{
return meta::find<ScriptComponent>();
}

}
