#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/StaticMeshComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> StaticMeshComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(StaticMeshComponent::Construct());
},true,{})

.Create<StaticMeshComponent>("StaticMeshComponent","",{});
}();


std::shared_ptr<meta::Metadata> StaticMeshComponent::GetMeta() const
{
return meta::find<StaticMeshComponent>();
}

}
