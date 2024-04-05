// META_IGNORE
#pragma once
#include <memory>
#include <unordered_map>
#include "Metadata.hpp"
#include "aerox/log.hpp"

#include <ranges>

namespace aerox::meta {
class _INTERNAL_TYPE_FACTORY
{

  
public:
  std::unordered_map<Type,std::shared_ptr<Metadata>,Type::HashFunction> classes;
  std::unordered_map<std::string,Type> aliases;
};

inline std::shared_ptr<_INTERNAL_TYPE_FACTORY> _internalGetFactory();
    
    
    
std::vector<std::string> names();

std::vector<Type> types();

std::vector<std::shared_ptr<Metadata>> values();

std::shared_ptr<Metadata> find(const Type& type);


template<typename T>
inline std::shared_ptr<Metadata> find()
{
  return find(Type::Infer<T>());
}

template<typename T>
inline std::shared_ptr<Metadata> find(T instance)
{
  return find(Type::Infer<T>());
}

std::shared_ptr<Metadata> find(const std::string& name);

struct TypeBuilder
{
  std::vector<std::shared_ptr<Field>> fields;
        
  TypeBuilder& AddField(const std::shared_ptr<Field>& field,const std::set<std::string>& tags = {});

  template<typename TClass,typename  TPropertyType>
  TypeBuilder& AddProperty(const std::string& name,TProperty<TClass,TPropertyType> prop,const std::set<std::string>& tags = {});
        
  TypeBuilder& AddFunction(const std::string& name, const functionTypedef& caller,bool isStatic,const std::set<std::string>& tags = {});
        

  template <typename T>
  std::shared_ptr<Metadata> Create(const std::string& name,const std::string& super = "",const std::set<std::string>& tags = {});
};

template <typename TClass, typename TPropertyType>
TypeBuilder& TypeBuilder::AddProperty(const std::string& name, TProperty<TClass, TPropertyType> prop,const std::set<std::string>& tags)
{
  return AddField(std::make_shared<Property>(Type::Infer<TClass>(),name,makeAccessor(prop)),tags);
}

template <typename T>
std::shared_ptr<Metadata> TypeBuilder::Create(const std::string& name,const std::string& super,const std::set<std::string>& tags ) {
  auto result = std::make_shared<Metadata>(Type::Infer<T>(),name,super,fields);
  result->FillTagsFrom(tags);
        
  const auto factory = _internalGetFactory();
  if(!factory->aliases.contains(result->GetName())) {
    factory->classes.emplace(result->GetType(),result);
    factory->aliases.emplace(result->GetName(),result->GetType());
    log::meta->Info("Registered type with alias {}",result->GetName());
  } else {
    log::meta->Error("Alias {} is already in use",result->GetName());
  }
  
  return result;
}
}
