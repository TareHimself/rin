#include "aerox/core/meta/MetaType.hpp"
namespace aerox
{
    MetaType::MetaType()
    {
        _id = "";
        _size = 0;
    }

    MetaType::MetaType(const std::string& id, const size_t& size)
    {
        _id = id;
        _size = size;
    }

    std::string MetaType::GetId() const
    {
        return _id;
    }

    size_t MetaType::GetSize() const
    {
        return _size;
    }
}
