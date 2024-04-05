#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/CameraComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> CameraComponent::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(CameraComponent::Construct());
},true,{})

.Create<CameraComponent>("CameraComponent","",{});
}();


std::shared_ptr<meta::Metadata> CameraComponent::GetMeta() const
{
return meta::find<CameraComponent>();
}

}
