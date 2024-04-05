#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/Font.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> Font::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(Font::Construct());
},true,{})

.Create<Font>("Font","",{});
}();


std::shared_ptr<meta::Metadata> Font::GetMeta() const
{
return meta::find<Font>();
}

}
