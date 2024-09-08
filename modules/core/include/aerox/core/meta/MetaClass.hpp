#pragma once
#include <vector>

#include "MetaFunction.hpp"
#include "MetaProperty.hpp"
#include "MetaType.hpp"
#include "aerox/core/memory.hpp"
namespace aerox
{
    class MetaClass {
        MetaType _type{};
        std::string _name{};
        Shared<MetaClass> _super{};
        std::vector<Shared<MetaProperty>> _properties{};
        std::vector<Shared<MetaFunction>> _functions{};
    public:
        [[nodiscard]] std::vector<Shared<MetaProperty>> GetProperties() const;
        [[nodiscard]] std::vector<Shared<MetaFunction>> GetFunctions() const;
        [[nodiscard]] std::string GetName() const;

        [[nodiscard]] MetaType GetType() const;

        MetaClass(const MetaType& type,const std::string& name,const std::vector<Shared<MetaProperty>>& properties,const std::vector<Shared<MetaFunction>>& functions);


    };
}
