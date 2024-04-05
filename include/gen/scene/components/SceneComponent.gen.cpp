#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/SceneComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> SceneComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(SceneComponent::Construct());
},true,{})

.Create<SceneComponent>("SceneComponent","",{});
}();


std::shared_ptr<meta::Metadata> SceneComponent::GetMeta() const
{
return meta::find<SceneComponent>();
}

}
