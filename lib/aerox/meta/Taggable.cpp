#include "aerox/meta/Taggable.hpp"

#include "aerox/meta/Utils.hpp"

#include <vscript/utils.hpp>

namespace aerox::meta
{
   json Taggable::GetTags() const
    {
        return _tags;
    }

    json Taggable::GetTag(const std::string &tag) {
     return _tags[tag];
    }

    void Taggable::AddTag(const std::string &tag, const json &data) {
     _tags[tag] = data;
    }

    void Taggable::RemoveTag(const std::string& tag)
    {
        _tags.erase(tag);
    }

    bool Taggable::HasTag(const std::string& tag) const
    {
        return _tags.contains(tag);
    }

    void Taggable::FillTagsFrom(const std::set<std::string> &tags) {
     for(auto &tag : tags) {
       if(tag.find('=') == std::string::npos) {
         _tags[tag] = "";
       } else {
         std::vector<std::string> parts;
         vs::split(parts,tag,"=");
         _tags[parts.front()] = ParseTagValue(vs::join(std::vector(parts.begin() + 1,parts.end()),"="));
       }
     }
    }

    json Taggable::ParseTagValue(const std::string &val) {
      if(val.starts_with('[')) {
        std::vector<json> res;
        std::vector<std::string> parts;
        vs::split(parts,std::string(val.begin() + 1,val.end() - 1),",");
        res.reserve(parts.size());
        for(auto &part : parts) {
          res.push_back(ParseTagValue(part));
        }

        return res;
      }

      return val;
    }
}

