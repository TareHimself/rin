#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/PointLightComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> PointLightComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(PointLightComponent::Construct());
},true,{})

.Create<PointLightComponent>("PointLightComponent","",{});
}();


std::shared_ptr<meta::Metadata> PointLightComponent::GetMeta() const
{
return meta::find<PointLightComponent>();
}

}
