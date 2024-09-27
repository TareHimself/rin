#pragma once
#include <unordered_map>
#include <string>

#include "MetaType.hpp"
#include "rin/core/memory.hpp"
class MetaFunction;
class MetaProperty;
class MetaClass;

class MetaFactory {
    std::unordered_map<std::string,Shared<MetaClass>> _classesNamesToClass{};
    std::unordered_map<std::string,Shared<MetaClass>> _classesIdsToClass{};

public:
    static Shared<MetaFactory> Get();

    template<typename T>
    Shared<MetaClass> RegisterClass(const std::string& name,const std::vector<Shared<MetaProperty>>& properties,const std::vector<Shared<MetaFunction>>& functions);

    Shared<MetaClass> RegisterClass(const MetaType& type,const std::string& name,const std::vector<Shared<MetaProperty>>& properties,const std::vector<Shared<MetaFunction>>& functions);

    Shared<MetaClass> RegisterClass(const Shared<MetaClass>& cls);

    template<typename T>
    Shared<MetaClass> FindClass();

    Shared<MetaClass> FindClass(const std::string& className);
};

template <typename T>
Shared<MetaClass> MetaFactory::RegisterClass(const std::string& name,
    const std::vector<Shared<MetaProperty>>& properties, const std::vector<Shared<MetaFunction>>& functions)
{
    return RegisterClass(MetaType::Infer<T>(),name,properties,functions);
}

template <typename T>
Shared<MetaClass> MetaFactory::FindClass()
{
    auto t = MetaType::Infer<T>();
    if(!_classesIdsToClass.contains(t.GetId())) return {};
    return _classesIdsToClass[t.GetId()];
}

template<typename T>
Shared<MetaClass> findMetaClass()
{
    return MetaFactory::Get()->FindClass<T>();
}


Shared<MetaClass> findMetaClass(const std::string& className);
