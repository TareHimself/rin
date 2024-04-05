#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/Texture.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> Texture::Meta = [](){
return meta::TypeBuilder()
.AddProperty("_filter",&Texture::_filter,{})
.AddProperty("_format",&Texture::_format,{})
.AddProperty("_size",&Texture::_size,{})
.AddProperty("_mipMapped",&Texture::_mipMapped,{})
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(Texture::Construct());
},true,{})

.Create<Texture>("Texture","",{});
}();


std::shared_ptr<meta::Metadata> Texture::GetMeta() const
{
return meta::find<Texture>();
}

}
