#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "Test.hpp"

std::shared_ptr<meta::Metadata> TestGameObject::Meta = [](){
return meta::TypeBuilder()
.AddProperty("_mesh",&TestGameObject::_mesh,{})
.AddProperty("_meshComponent",&TestGameObject::_meshComponent,{})
.AddProperty("_scriptComp",&TestGameObject::_scriptComp,{})
.AddFunction("GetWorldTransform",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<TestGameObject*>(instance)->GetWorldTransform());
},false,{})

.AddFunction("Construct",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(TestGameObject::Construct());
},true,{})

.Create<TestGameObject>("TestGameObject","scene",{});
}();


std::shared_ptr<meta::Metadata> TestGameObject::GetMeta() const
{
return meta::find<TestGameObject>();
}
