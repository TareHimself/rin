#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/audio/LiveAudio.hpp"

namespace aerox::audio {

std::shared_ptr<meta::Metadata> LiveAudio::Meta = [](){
return meta::TypeBuilder()
.AddFunction("Play",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<LiveAudio*>(instance)->Play());
},false,{})

.AddFunction("Pause",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<LiveAudio*>(instance)->Pause());
},false,{})

.AddFunction("Seek",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
double &arg_0 = args[0]; \
 
static_cast<LiveAudio*>(instance)->Seek(arg_0);
return meta::Value();
},false,{})

.AddFunction("GetPosition",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<LiveAudio*>(instance)->GetPosition());
},false,{})

.AddFunction("GetLength",[](const meta::Reference& instance, const std::vector<meta::Reference>& args){
 
return meta::Value(static_cast<LiveAudio*>(instance)->GetLength());
},false,{})

.Create<LiveAudio>("LiveAudio","",{});
}();


std::shared_ptr<meta::Metadata> LiveAudio::GetMeta() const
{
return meta::find<LiveAudio>();
}

}
