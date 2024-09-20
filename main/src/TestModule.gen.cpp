#include "TestModule.gen.hpp"
#include "TestModule.hpp"
Shared<MetaClass> Foo::Meta = [](){
auto type = MetaFactory::Get()->RegisterClass<Foo>("Foo",{},{});

return type;
}();

Shared<MetaClass> Foo::GetMeta() const { return Meta; }
Shared<MetaClass> Foo::Meta = [](){
auto type = MetaFactory::Get()->RegisterClass<Foo>("Foo",{},{});

return type;
}();

Shared<MetaClass> Foo::GetMeta() const { return Meta; }
