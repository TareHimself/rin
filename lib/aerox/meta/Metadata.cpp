#include "aerox/meta/Metadata.hpp"

#include "aerox/utils.hpp"
#include "aerox/meta/Factory.hpp"

#include <ranges>

namespace aerox::meta
{
    Metadata::Metadata()
    {
        _type = {};
        _name = "";
    }

    Metadata::Metadata(const Type& type, const std::string& name,const std::string& super, const std::vector<std::shared_ptr<Field>>& fields,
        const std::set<std::string>& tags)
    {
        _type = type;
        _name = name;
        _super = super;
        FillTagsFrom(tags);
        for (auto& field : fields)
        {
          if(field->GetType() == EFieldType::FieldFunction) {
            _functions[field->GetName()] = utils::cast<Function>(field);
          }
          else if(field->GetType() == EFieldType::FieldProperty) {
            _properties[field->GetName()] = utils::cast<Property>(field);
          }
        }
    }

    Type Metadata::GetType() const
    {
        return _type;
    }

    std::string Metadata::GetName() const
    {
        return _name;
    }

    std::vector<std::shared_ptr<Property>> Metadata::GetProperties() const {
      std::vector<std::shared_ptr<Property>> d;
      d.reserve(_properties.size());
      
      for(const auto &val : _properties | std::views::values) {
        d.push_back(val);
      }
      
      if(const auto super = GetSuper()) {
        auto superItems = super->GetProperties();
        d.insert(d.end(),superItems.begin(),superItems.end());
      }
      

      return d;
    }

    std::vector<std::shared_ptr<Function>> Metadata::GetFunctions() const {
      std::vector<std::shared_ptr<Function>> d;
      d.reserve(_functions.size());
      
      for(const auto &val : _functions | std::views::values) {
        d.push_back(val);
      }

      if(const auto super = GetSuper()) {
        auto superItems = super->GetFunctions();
        d.insert(d.end(),superItems.begin(),superItems.end());
      }

      return d;
    }

    std::shared_ptr<Property> Metadata::FindProperty(const std::string& id)
    {
        if (_properties.contains(id))
        {
            return _properties[id];
        }

        if(const auto super = GetSuper()) {
          return super->FindProperty(id);
        }

        return {};
    }

    std::shared_ptr<Function> Metadata::FindFunction(const std::string& id)
    {
      if (_functions.contains(id))
      {
        return _functions[id];
      }

      if(const auto super = GetSuper()) {
        return super->FindFunction(id);
      }
      
      return {};
    }

    bool Metadata::HasProperty(const std::string &id) const {
      if(_properties.contains(id)) {
        return true;
      }

      if(const auto super = GetSuper()) {
        return super->HasProperty(id);
      }

      return false;
    }

    bool Metadata::HasFunction(const std::string &id) const {
      if(_functions.contains(id)) {
        return true;
      }

      if(const auto super = GetSuper()) {
        return super->HasFunction(id);
      }

      return false;
    }

    std::shared_ptr<Metadata> Metadata::GetSuper() const {
      if(_super.empty()) {
        return {};
      }

      return find(_super);
    }
}
