#include "rin/core/meta/MetaClass.hpp"
std::vector<Shared<MetaProperty>> MetaClass::GetProperties() const
{
    return _properties;
}

std::vector<Shared<MetaFunction>> MetaClass::GetFunctions() const
{
    return _functions;
}

std::string MetaClass::GetName() const
{
    return _name;
}

MetaType MetaClass::GetType() const
{
    return _type;
}

MetaClass::MetaClass(const MetaType& type, const std::string& name, const std::vector<Shared<MetaProperty>>& properties,
                     const std::vector<Shared<MetaFunction>>& functions)
{
    _type = type;
    _name = name;
    _properties = properties;
    _functions = functions;
}
