#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/objects/DefaultCamera.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> DefaultCamera::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(DefaultCamera::Construct());
},true,{})

.Create<DefaultCamera>("DefaultCamera","",{});
}();


std::shared_ptr<meta::Metadata> DefaultCamera::GetMeta() const
{
return meta::find<DefaultCamera>();
}

}
