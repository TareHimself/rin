#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/objects/DirectionalLight.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> DirectionalLight::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(DirectionalLight::Construct());
},true,{})

.Create<DirectionalLight>("DirectionalLight","",{});
}();


std::shared_ptr<meta::Metadata> DirectionalLight::GetMeta() const
{
return meta::find<DirectionalLight>();
}

}
