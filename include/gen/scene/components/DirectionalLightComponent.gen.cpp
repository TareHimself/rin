#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/DirectionalLightComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> DirectionalLightComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(DirectionalLightComponent::Construct());
},true,{})

.Create<DirectionalLightComponent>("DirectionalLightComponent","",{});
}();


std::shared_ptr<meta::Metadata> DirectionalLightComponent::GetMeta() const
{
return meta::find<DirectionalLightComponent>();
}

}
