// META_IGNORE
#pragma once
#include <memory>
#include <vector>
#include "Field.hpp"
#include "Function.hpp"
#include "Property.hpp"
#include "Type.hpp"
#include "aerox/typedefs.hpp"

namespace aerox::meta
{

    struct Metadata : Taggable
    {
    protected:
        std::string _name;
        Type _type;
        std::string _super;
        std::unordered_map<std::string,std::shared_ptr<Property>> _properties;
        std::unordered_map<std::string,std::shared_ptr<Function>> _functions;
    public:
        Metadata();

        Metadata(const Type& type, const std::string& name,const std::string& super = "", const std::vector<std::shared_ptr<Field>>& fields = {},const std::set<std::string>& tags = {});
        [[nodiscard]] Type GetType() const;
        [[nodiscard]] std::string GetName() const;
        [[nodiscard]] std::vector<std::shared_ptr<Property>> GetProperties() const;
        [[nodiscard]] std::vector<std::shared_ptr<Function>> GetFunctions() const;
        std::shared_ptr<Property> FindProperty(const std::string& id);
        std::shared_ptr<Function> FindFunction(const std::string& id);
        bool HasProperty(const std::string& id) const;
        bool HasFunction(const std::string& id) const;
        std::shared_ptr<Metadata> GetSuper() const;
    };
}
