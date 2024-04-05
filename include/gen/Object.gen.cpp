#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../aerox/Object.hpp"

namespace aerox {

std::shared_ptr<meta::Metadata> Object::Meta = [](){
return meta::TypeBuilder()
.AddFunction("ToJson",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<Object*>(instance)->ToJson());
},false,{})

.Create<Object>("Object","",{});
}();


std::shared_ptr<meta::Metadata> Object::GetMeta() const
{
return meta::find<Object>();
}

}
