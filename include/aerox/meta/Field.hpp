// META_IGNORE
#pragma once
#include <string>
#include <set>
#include "Taggable.hpp"

namespace aerox::meta
{
    enum EFieldType
    {
        FieldUnspecified,
        FieldProperty,
        FieldFunction,
        FieldEnumMember,
    };

    struct Field : Taggable
    {
    protected:
        std::string _name;
        EFieldType _fieldType = EFieldType::FieldUnspecified;
    public:
        std::string GetName() const;
        EFieldType GetType() const;
        
    };
}
