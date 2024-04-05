#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/objects/PointLight.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> PointLight::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(PointLight::Construct());
},true,{})

.Create<PointLight>("PointLight","",{});
}();


std::shared_ptr<meta::Metadata> PointLight::GetMeta() const
{
return meta::find<PointLight>();
}

}
