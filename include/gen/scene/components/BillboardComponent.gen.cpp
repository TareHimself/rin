#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/BillboardComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> BillboardComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(BillboardComponent::Construct());
},true,{})

.Create<BillboardComponent>("BillboardComponent","",{});
}();


std::shared_ptr<meta::Metadata> BillboardComponent::GetMeta() const
{
return meta::find<BillboardComponent>();
}

}
