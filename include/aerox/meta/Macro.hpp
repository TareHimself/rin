// META_IGNORE
#pragma once

namespace aerox::meta
{
#ifndef META_MACROS
#define META_MACROS

#define META_SERIALIZATION_KEY_PROPERTIES "properties"
#define META_SERIALIZATION_KEY_ID "id"
#define META_SERIALIZATION_KEY_DATA "data"

#define META_TYPE_SERIALIZATION(IS_STATIC,SERIALIZER,DESERIALIZER) \
.AddFunction("ToJson",[](const Value& instance, const std::vector<Reference>& args) SERIALIZER,IS_STATIC) \
.AddFunction("FromJson",[](const Value& instance, const std::vector<Reference>& args) DESERIALIZER,IS_STATIC)


#define META_SIMPLE_STATIC_SERIALIZATION(TYPE,KEY,CONVERT_TO,CONVERT_FROM) \
META_TYPE_SERIALIZATION(true,{ \
    json d{}; \
    TYPE &val = args[0]; \
    d[META_SERIALIZATION_KEY_ID] = find(val)->GetName(); \
    d[KEY] = CONVERT_TO; \
    return Value(d); \
  },{ \
    json d = args[0]; \
    Reference a = args[1]; \
    auto id = find(a.GetType())->GetName(); \
    std::string storedId = d[META_SERIALIZATION_KEY_ID]; \
    utils::vassert(storedId == id,"Id Mismatch, expected '{}' but got '{}'",id,storedId); \
    TYPE &b = a; \
    json val = d[KEY]; \
    b = CONVERT_FROM; \
    return Value{}; \
  })

#define META_PRIMITIVE(TYPE) \
TypeBuilder() \
META_TYPE_SERIALIZATION(true,{ \
Reference val = args[0]; \
json j; \
TYPE& data = val; \
j[META_SERIALIZATION_KEY_ID] = find(val.GetType())->GetName(); \
j[META_SERIALIZATION_KEY_DATA] = data;  \
return Value(j); \
},{ \
json &j = args[0]; \
Reference target = args[1]; \
TYPE &data = target; \
auto id  = find(target.GetType())->GetName(); \
std::string storedId = j[META_SERIALIZATION_KEY_ID]; \
utils::vassert(storedId == id,"Id Mismatch, expected '{}' but got '{}'",id,storedId); \
data = j[META_SERIALIZATION_KEY_DATA]; \
return Value(); \
}) \
.Create<TYPE>(#TYPE);

#define META_CONCAT_IMPL(x, y) x##y

#define META_CONCAT(x, y) META_CONCAT_IMPL(x, y)

// Marks the type below it for reflection
#define META_TYPE(...)

// Marks the variable below it for reflection
#define META_PROPERTY(...)
    
// Marks the function below it for reflection
#define META_FUNCTION(...)

// Marks the constructor below it for reflection not yet parsed
#define META_CONSTRUCTOR(...)

#define META_CREATE_PROPERTY(Owner,Name,PropertyType) manage<vengine::meta::Property>( \
vengine::meta::Type::Infer<PropertyType>(),   \
#Name,  \
makeAccessor(&Owner::Name))

#define META_CREATE_FUNCTION(Name) manage<vengine::meta::Function>(#Name,[](const vengine::meta::Value& result,const vengine::meta::Value& instance, const std::vector<vengine::meta::Value>& args)

#define META_JOIN_IMPL(A,B) A##B
    
#define META_JOIN(A,B) META_JOIN_IMPL(A,B)

#ifdef _MSC_VER
#define META_TYPE_NAME __FUNCSIG__
#else
#define META_TYPE_NAME __PRETTY_FUNCTION__
#endif
    

#define META_DECLARE(ID)  \
static void META_JOIN(_META_define_function_,ID)(); \
namespace { \
struct META_JOIN(_META_define_struct_,ID) {  \
META_JOIN(_META_define_struct_,ID)() { \
META_JOIN(_META_define_function_,ID)(); \
} \
};  \
} \
static const META_JOIN(_META_define_struct_,ID) META_JOIN(_META_define_,ID);  \
static void META_JOIN(_META_define_function_,ID)()

#define META_IMPLEMENT(Type)  \
META_DECLARE(Type) \
{   \
    _META_GENERATE_##Type \
}   

#endif

// For classes and structs
#define META_BODY() META_JOIN(_meta_,META_JOIN(META_FILE_ID,META_JOIN(_,__LINE__)))()
}