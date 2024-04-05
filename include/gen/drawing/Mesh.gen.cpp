#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/Mesh.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> Mesh::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(Mesh::Construct());
},true,{})

.Create<Mesh>("Mesh","",{});
}();


std::shared_ptr<meta::Metadata> Mesh::GetMeta() const
{
return meta::find<Mesh>();
}

}
