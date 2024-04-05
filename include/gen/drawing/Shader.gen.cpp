#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/Shader.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> Shader::Meta = [](){
return meta::TypeBuilder()
.Create<Shader>("Shader","",{});
}();


std::shared_ptr<meta::Metadata> Shader::GetMeta() const
{
return meta::find<Shader>();
}

}
