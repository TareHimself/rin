#include "aerox/core/meta/MetaFactory.hpp"
#include "aerox/core/meta/MetaClass.hpp"
namespace aerox
{
    Shared<MetaFactory> MetaFactory::Get()
    {
        static Shared<MetaFactory> factory = std::make_shared<MetaFactory>();
        return factory;
    }

    Shared<MetaClass> MetaFactory::RegisterClass(const MetaType& type, const std::string& name,
        const std::vector<Shared<MetaProperty>>& properties, const std::vector<Shared<MetaFunction>>& functions)
    {
        return RegisterClass(std::make_shared<MetaClass>(type,name,properties,functions));
    }

    Shared<MetaClass> MetaFactory::RegisterClass(const Shared<MetaClass>& cls)
    {
        _classesNamesToClass.emplace(cls->GetName(),cls);
        _classesIdsToClass.emplace(cls->GetType().GetId(),cls);
        return cls;
    }

    Shared<MetaClass> MetaFactory::FindClass(const std::string& className)
    {
        if(!_classesNamesToClass.contains(className)) return {};
        return _classesNamesToClass[className];
    }

    Shared<MetaClass> findMetaClass(const std::string& className)
    {
        return MetaFactory::Get()->FindClass(className);
    }
}
