// META_IGNORE
#pragma once
#include "aerox/json.hpp"
#include <set>
#include <string>

namespace aerox::meta
{
    struct Taggable
    {
      
    protected:
        json _tags;
    public:
        virtual ~Taggable() = default;
        json GetTags() const;
        json GetTag(const std::string& tag);
        void AddTag(const std::string& tag,const json & data);
        void RemoveTag(const std::string& tag);
        bool HasTag(const std::string& tag) const;
        void FillTagsFrom(const std::set<std::string>& tags);
        static json ParseTagValue(const std::string& val);
    
    };

}
