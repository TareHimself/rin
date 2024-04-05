#include "aerox/meta/Utils.hpp"

namespace aerox::meta::utils
{
    std::vector<std::string> split(const std::string& str,const std::string& sep)
    {
        std::vector<std::string> result;
        std::string remaining = str.substr();
        auto pos = remaining.find_first_of(sep);
        while(pos != std::string::npos)
        {
            result.push_back(remaining.substr(0,pos));
            remaining = remaining.substr(pos + sep.size());
            pos = remaining.find_first_of(sep);
        }

        if(!remaining.empty())
        {
            result.push_back(remaining);
        }

        return result;
    }

    size_t count(const std::string& str, const std::string& search)
    {
        size_t numFound = 0;
        
        std::string remaining = str.substr();
        auto pos = remaining.find_first_of(search);
        while(pos != std::string::npos)
        {
            numFound++;
            remaining = remaining.substr(pos + search.size());
            pos = remaining.find_first_of(search);
        }

        return numFound;
    }

    std::string trim(const std::string& str)
    {
        auto copied = str;
        copied.erase(copied.find_last_not_of(' ')+1);         //suffixing spaces
        copied.erase(0, copied.find_first_not_of(' '));       //prefixing spaces
        return copied;
    }
}
