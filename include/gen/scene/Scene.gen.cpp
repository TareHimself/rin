#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/scene/Scene.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> Scene::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(Scene::Construct());
},true,{})

.AddProperty("testProp",&Scene::testProp,{})
.Create<Scene>("Scene","",{});
}();


std::shared_ptr<meta::Metadata> Scene::GetMeta() const
{
return meta::find<Scene>();
}

}
