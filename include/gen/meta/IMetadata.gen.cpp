#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/meta/IMetadata.hpp"

namespace aerox::meta {

std::shared_ptr<meta::Metadata> IMetadata::Meta = [](){
return meta::TypeBuilder()
.AddFunction("ToJson",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<IMetadata*>(instance)->ToJson());
},false,{})

.AddFunction("FromJson",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
json  &arg_0 = args[0]; \
 
static_cast<IMetadata*>(instance)->FromJson(arg_0);
return meta::Value();
},false,{})

.Create<IMetadata>("IMetadata","",{});
}();


std::shared_ptr<meta::Metadata> IMetadata::GetMeta() const
{
return meta::find<IMetadata>();
}

}
